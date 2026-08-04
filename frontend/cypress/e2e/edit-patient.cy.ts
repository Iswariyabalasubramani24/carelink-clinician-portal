describe('Patient edit flow', () => {
  const uniqueSuffix = Date.now();
  const testPatient = {
    mrn: `E2E-${uniqueSuffix}`,
    firstName: 'EditMe',
    lastName: `Original${uniqueSuffix}`,
    dob: '1980-05-20',
    deviceType: 'ICD',
    serial: `E2E-SN-${uniqueSuffix}`,
    implantDate: '2022-06-01'
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
  });

  it('opens the Edit modal pre-filled, saves changes, and they persist across reload', () => {
    cy.contains('button', 'Edit').click();
    cy.get('.modal-panel').should('be.visible');
    cy.contains('h2', 'Edit Patient').should('be.visible');

    // Pre-filled with the current values.
    cy.get('#editFirstName').should('have.value', testPatient.firstName);
    cy.get('#editMrn').should('have.value', testPatient.mrn);

    const updatedLastName = `Updated${uniqueSuffix}`;
    const updatedPhone = '+1-555-0199';

    cy.get('#editLastName').clear().type(updatedLastName);
    cy.get('#editPhone').clear().type(updatedPhone);
    cy.get('#editDeviceType').select('Pacemaker');
    cy.contains('.edit-patient-form button', 'Save Changes').click();

    cy.contains('Patient updated successfully.', { timeout: 10000 }).should('be.visible');
    cy.get('.modal-backdrop', { timeout: 5000 }).should('not.exist');

    // The header and Overview tab reflect the change immediately.
    cy.contains('h1', `${testPatient.firstName} ${updatedLastName}`).should('be.visible');
    cy.contains('button', 'Overview').click();
    cy.contains('dd', updatedPhone).should('be.visible');

    // Confirm it was actually persisted server-side, not just local UI state.
    cy.reload();
    cy.contains('h1', `${testPatient.firstName} ${updatedLastName}`, { timeout: 10000 }).should('be.visible');
    cy.contains('button', 'Equipment').click();
    cy.contains('dd', 'Pacemaker').should('be.visible');
  });

  it('shows validation errors and does not save when a required field is cleared', () => {
    cy.contains('button', 'Edit').click();
    cy.get('#editFirstName').clear();
    cy.contains('.edit-patient-form button', 'Save Changes').click();

    cy.contains('First name is required.').scrollIntoView().should('be.visible');
    cy.contains('Patient updated successfully.').should('not.exist');
  });

  it('closing the modal without saving discards changes', () => {
    cy.contains('button', 'Edit').click();
    cy.get('#editLastName').clear().type('ShouldNotPersist');
    cy.get('.edit-patient-form .icon-btn').click();

    cy.get('.modal-backdrop').should('not.exist');
    cy.contains('h1', testPatient.lastName).should('be.visible');
  });
});
