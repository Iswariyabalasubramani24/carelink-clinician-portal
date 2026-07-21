describe('A.2: Patient Detail Profile tab and Schedule tab (clinic-default / override)', () => {
  const uniqueSuffix = Date.now();
  const testPatient = {
    mrn: `E2E-${uniqueSuffix}`,
    firstName: 'ProfileSchedule',
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
  });

  it('shows the read-only patient identity fields on the Profile tab', () => {
    cy.contains('button', 'Profile').click();

    cy.contains('dt', 'First Name').next('dd').should('contain.text', testPatient.firstName);
    cy.contains('dt', 'Last Name').next('dd').should('contain.text', testPatient.lastName);
    cy.contains('dt', 'MRN').next('dd').should('contain.text', testPatient.mrn);
    cy.contains('dt', 'Status').next('dd').should('contain.text', 'Active');
  });

  it('shows the Schedule tab defaulting to the clinic-wide interval with a read-only input, then toggling to an override that survives a reload', () => {
    cy.contains('button', 'Schedule').click();

    cy.contains('.toggle-option--active', 'Use clinic default', { timeout: 10000 }).should('exist');
    cy.get('#scheduleIntervalDays').should('have.attr', 'readonly');

    cy.contains('.toggle-option', 'Override for this patient').click();
    cy.get('#scheduleIntervalDays').should('not.have.attr', 'readonly');
    cy.get('#scheduleIntervalDays').clear().type('45');

    cy.contains('.care-alert-actions button', 'Save').click();
    cy.contains('Saved.', { timeout: 10000 }).should('be.visible');

    // Confirm the override was actually persisted server-side, not just local UI state.
    cy.reload();
    cy.contains('button', 'Schedule').click();
    cy.contains('.toggle-option--active', 'Override for this patient', { timeout: 10000 }).should('exist');
    cy.get('#scheduleIntervalDays').should('have.value', '45');
  });
});
