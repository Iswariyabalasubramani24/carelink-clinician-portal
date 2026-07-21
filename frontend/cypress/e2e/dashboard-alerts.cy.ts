describe('Sprint 4 + 5: Dashboard widgets, patient detail tabs, and alerts', () => {
  // A fresh patient always has LastSyncedAt = null, which deterministically
  // triggers a Disconnected Monitor alert - unlike a shared seeded patient,
  // this fixture can't have its alert already permanently acknowledged by an
  // earlier run (acknowledgment is - correctly - permanent, so it never
  // resurfaces on its own).
  const uniqueSuffix = Date.now();
  const testPatient = {
    mrn: `E2E-${uniqueSuffix}`,
    firstName: 'AlertFixture',
    lastName: `Patient${uniqueSuffix}`,
    dob: '1970-01-01',
    deviceType: 'ICD',
    serial: `E2E-SN-${uniqueSuffix}`,
    implantDate: '2023-01-10'
  };

  beforeEach(() => {
    cy.clearCookies();
    localStorage.clear();
    cy.visit('/login');
    cy.get('#email').type('doctor@apollo.com');
    cy.get('#password').type('Test@123');
    cy.contains('button', 'Sign in').click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/dashboard');
  });

  it('shows dashboard widgets including the alert count, then walks through a patient with an active alert: banner acknowledge, History charts, and a CareAlert override that survives a reload', () => {
    // Dashboard shows all four widgets, including the new Active Alerts one.
    cy.contains('h1', 'Home Dashboard').should('be.visible');
    cy.contains('.widget-card__label', 'New Patients This Week').should('be.visible');
    cy.contains('.widget-card__label', 'Disconnected Monitors').should('be.visible');
    cy.contains('.widget-card__label', 'Total Active Patients').should('be.visible');
    cy.contains('.widget-card__label', 'Active Alerts').should('be.visible');
    cy.get('.widget-card__count').should('have.length', 4);

    // Create a fresh patient (never synced -> guaranteed active Disconnected
    // Monitor alert) and open their detail page.
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

    cy.contains('.patient-name', `${testPatient.firstName} ${testPatient.lastName}`).click();
    cy.location('pathname', { timeout: 10000 }).should('match', /\/patients\/\d+$/);
    cy.contains('h1', testPatient.firstName).should('be.visible');

    // Overview tab shows a contextual alert banner with Acknowledge/Snooze actions
    cy.get('.alert-banner', { timeout: 10000 }).should('have.length.greaterThan', 0);
    cy.contains('.alert-banner', 'Notice of Disconnected Monitor').within(() => {
      cy.contains('button', 'Acknowledge').should('be.visible');
      cy.contains('button', 'Snooze (15 days)').should('be.visible');
    });

    // Acknowledge every banner until none remain. Each click fires an async
    // HTTP call before Angular removes the banner, so wait for the DOM to
    // actually reflect one fewer banner before checking again - otherwise the
    // next iteration can race ahead of the still-in-flight removal.
    const acknowledgeAllBanners = (): void => {
      cy.get('body').then(($body) => {
        const remaining = $body.find('.alert-banner').length;
        if (remaining > 0) {
          cy.get('.alert-banner').first().within(() => {
            cy.contains('button', 'Acknowledge').click();
          });
          cy.get('.alert-banner').should('have.length', remaining - 1);
          acknowledgeAllBanners();
        }
      });
    };
    acknowledgeAllBanners();
    cy.get('.alert-banner').should('not.exist');

    // History tab - Sprint 4 regression check: charts still render correctly
    cy.contains('button', 'History').click();
    cy.get('canvas', { timeout: 10000 }).should('have.length', 2);

    // CareAlert Notification tab - toggle to an override and save
    cy.contains('button', 'CareAlert Notification').click();
    cy.get('.urgency-badge', { timeout: 10000 }).should('have.length', 3);
    cy.get('.urgency-select').should('have.length', 0);

    cy.contains('.toggle-option', 'Override for this patient').click();
    cy.get('.urgency-select').should('have.length', 3);
    cy.get('.urgency-select').first().select('Red');
    cy.contains('button', 'Save').click();
    cy.contains('Saved.', { timeout: 10000 }).should('be.visible');

    // Confirm the override was actually persisted server-side, not just local UI state
    cy.reload();
    cy.contains('button', 'CareAlert Notification').click();
    cy.contains('.toggle-option--active', 'Override for this patient', { timeout: 10000 }).should('exist');
  });
});
