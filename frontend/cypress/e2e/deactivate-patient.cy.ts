describe('Patient deactivate/reactivate flow', () => {
  // Computed fresh per test (not once at describe level) so the two tests'
  // patients never share a name/MRN - a shared identifier let a previous
  // test's leftover patient be the one cy.contains('.patient-name', ...)
  // matched, silently toggling the wrong record.
  let testPatient: {
    mrn: string;
    firstName: string;
    lastName: string;
    dob: string;
    deviceType: string;
    serial: string;
    implantDate: string;
  };

  beforeEach(() => {
    const uniqueSuffix = `${Date.now()}${Math.floor(Math.random() * 1000)}`;
    testPatient = {
      mrn: `E2E-${uniqueSuffix}`,
      firstName: 'ToggleMe',
      lastName: `Patient${uniqueSuffix}`,
      dob: '1975-03-10',
      deviceType: 'Pacemaker',
      serial: `E2E-SN-${uniqueSuffix}`,
      implantDate: '2021-08-15'
    };

    cy.clearCookies();
    localStorage.clear();
    cy.visit('/login');
    cy.get('#email').type('doctor@apollo.com');
    cy.get('#password').type('Test@123');
    cy.contains('button', 'Sign in').click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/dashboard');

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
  });

  it('deactivates an active patient, then reactivates them, persisting across reload each time', () => {
    // Starts Active with a Deactivate action available.
    cy.get('.status-badge').should('contain.text', 'Active');
    cy.contains('.status-row button', 'Deactivate').click();

    cy.get('.status-badge', { timeout: 10000 }).should('contain.text', 'Inactive');
    cy.contains('.status-row button', 'Activate').should('be.visible');

    // Confirm the deactivation was actually persisted server-side.
    cy.reload();
    cy.get('.status-badge', { timeout: 10000 }).should('contain.text', 'Inactive');

    // Reactivate.
    cy.contains('.status-row button', 'Activate').click();
    cy.get('.status-badge', { timeout: 10000 }).should('contain.text', 'Active');
    cy.contains('.status-row button', 'Deactivate').should('be.visible');

    cy.reload();
    cy.get('.status-badge', { timeout: 10000 }).should('contain.text', 'Active');
  });

  it('an advanced-search filter for Inactive patients excludes an active patient and includes a deactivated one', () => {
    cy.contains('.status-row button', 'Deactivate').click();
    cy.get('.status-badge', { timeout: 10000 }).should('contain.text', 'Inactive');

    cy.contains('a', 'Patients').click();
    cy.get('.advanced-search__toggle').click();
    cy.get('#searchStatus').select('Inactive');
    cy.contains('.advanced-search__form button', 'Search').click();

    cy.get('tbody tr', { timeout: 10000 }).should('have.length.greaterThan', 0);
    cy.contains('.patient-name', `${testPatient.firstName} ${testPatient.lastName}`).should('be.visible');

    cy.get('#searchStatus').select('Active');
    cy.contains('.advanced-search__form button', 'Search').click();
    cy.contains('.patient-name', `${testPatient.firstName} ${testPatient.lastName}`).should('not.exist');
  });
});
