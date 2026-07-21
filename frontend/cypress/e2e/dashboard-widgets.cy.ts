describe('Dashboard: Quick Links by role, Recent Alerts click-through, and Upcoming Transmissions', () => {
  const uniqueSuffix = Date.now();

  function loginAs(email: string, password: string): void {
    cy.clearCookies();
    localStorage.clear();
    cy.visit('/login');
    cy.get('#email').type(email);
    cy.get('#password').type(password);
    cy.contains('button', 'Sign in').click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/dashboard');
  }

  it('shows Patients and Transmission Schedule quick links to a regular clinician, but hides admin-only links', () => {
    loginAs('doctor@apollo.com', 'Test@123');

    cy.get('.quick-link', { timeout: 10000 }).should('have.length', 2);
    cy.contains('.quick-link', 'Patients').should('be.visible');
    cy.contains('.quick-link', 'Transmission Schedule').should('be.visible');
    cy.contains('.quick-link', 'Clinic Management').should('not.exist');
    cy.contains('.quick-link', 'Audit Log').should('not.exist');
  });

  it('shows all four quick links to an Admin and navigates to Clinic Management and Audit Log', () => {
    loginAs('admin@apollo.com', 'Admin@123');

    cy.get('.quick-link', { timeout: 10000 }).should('have.length', 4);

    cy.contains('.quick-link', 'Clinic Management').click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/clinic-management');

    cy.visit('/dashboard');
    cy.contains('.quick-link', 'Audit Log', { timeout: 10000 }).click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/audit-log');
  });

  it('shows a freshly triggered alert in the Recent Alerts panel and navigates to the patient on click', () => {
    loginAs('doctor@apollo.com', 'Test@123');

    const testPatient = {
      mrn: `E2E-${uniqueSuffix}A`,
      firstName: 'RecentAlerts',
      lastName: `Patient${uniqueSuffix}`,
      dob: '1970-01-01',
      deviceType: 'ICD',
      serial: `E2E-ALERT-SN-${uniqueSuffix}`,
      implantDate: '2023-01-10'
    };

    cy.contains('a', 'Patients').click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/patients');
    cy.contains('button', '+ Add Patient').click();
    cy.get('.modal-panel').should('be.visible');
    cy.get('#mrn').type(testPatient.mrn);
    cy.get('#firstName').type(testPatient.firstName);
    cy.get('#lastName').type(testPatient.lastName);
    cy.get('#dob').type(testPatient.dob);
    cy.get('#deviceType').select(testPatient.deviceType);
    cy.get('#serial').type(testPatient.serial);
    cy.get('#implantDate').type(testPatient.implantDate);
    cy.contains('form button', 'Add Patient').click();
    cy.contains('Patient added successfully.').scrollIntoView().should('be.visible');
    cy.get('.modal-backdrop', { timeout: 5000 }).should('not.exist');

    // A never-synced patient deterministically has an active Disconnected
    // Monitor alert, which should now surface on the dashboard.
    cy.contains('a', 'Dashboard').click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/dashboard');

    cy.contains('.alert-list__item', `${testPatient.firstName} ${testPatient.lastName}`, { timeout: 10000 }).click();
    cy.location('pathname', { timeout: 10000 }).should('match', /\/patients\/\d+$/);
    cy.contains('h1', testPatient.firstName).should('be.visible');
  });

  it('renders upcoming transmissions with a working row link and a working "View All" link', () => {
    cy.intercept('GET', '**/api/v1/schedule/transmission-schedule*').as('transmissionSchedule');
    loginAs('doctor@apollo.com', 'Test@123');

    cy.contains('.panel__title', 'Upcoming Transmissions', { timeout: 10000 }).should('be.visible');

    // Branch on the server response rather than a DOM snapshot - a
    // cy.get('body').then() races the in-flight request and can pick the empty
    // branch just before the list renders. The panel shows only entries with a
    // non-null nextScheduledDate (never-synced patients are excluded), so
    // mirror that filter when deciding which branch to expect.
    cy.wait('@transmissionSchedule').its('response.body').then((entries) => {
      const upcoming = entries.filter((e: { nextScheduledDate: string | null }) => e.nextScheduledDate !== null);
      if (upcoming.length > 0) {
        cy.get('.transmission-list__item', { timeout: 10000 }).first().click();
        cy.location('pathname', { timeout: 10000 }).should('match', /\/patients\/\d+$/);
        cy.visit('/dashboard');
      } else {
        cy.contains('No upcoming transmissions.').should('be.visible');
      }
    });

    cy.contains('.panel__view-all', 'View All', { timeout: 10000 }).click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/transmission-schedule');
  });
});
