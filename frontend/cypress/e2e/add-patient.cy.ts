describe('Patient list and Add Patient flow', () => {
  const uniqueSuffix = Date.now();
  const testPatient = {
    mrn: `E2E-${uniqueSuffix}`,
    firstName: 'Cypress',
    lastName: `Tester${uniqueSuffix}`,
    dob: '1985-06-15',
    phone: '+1-555-0100',
    email: `cypress.tester${uniqueSuffix}@example.com`,
    deviceType: 'ICD',
    manufacturer: 'Medtronic',
    model: 'Evera XT',
    serial: `E2E-SN-${uniqueSuffix}`,
    implantDate: '2023-01-10'
  };

  beforeEach(() => {
    // Patients endpoints are now JWT-protected (Sprint 2) - log in first.
    cy.clearCookies();
    cy.visit('/login');
    cy.get('#email').type('doctor@apollo.com');
    cy.get('#password').type('Test@123');
    cy.contains('button', 'Sign in').click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/patients');
  });

  it('loads existing patients, validates required fields, and adds a new patient end-to-end', () => {
    // 1 & 2. Visit /patients and confirm the list loads with existing patients
    cy.contains('h1', 'Patients').should('be.visible');
    cy.get('tbody tr').should('have.length.greaterThan', 0);
    cy.contains('td', 'Rajesh Kumar').should('be.visible');

    // 3. Open the Add Patient modal
    cy.contains('button', '+ Add Patient').click();
    cy.get('.modal-panel').should('be.visible');

    // Optional: submit empty to confirm validation errors appear first
    cy.contains('form button', 'Add Patient').click();
    cy.contains('Medical record number is required.').scrollIntoView().should('be.visible');
    cy.contains('First name is required.').scrollIntoView().should('be.visible');
    cy.contains('Last name is required.').scrollIntoView().should('be.visible');
    cy.contains('Date of birth is required.').scrollIntoView().should('be.visible');
    cy.contains('Device type is required.').scrollIntoView().should('be.visible');
    cy.contains('Serial number is required.').scrollIntoView().should('be.visible');
    cy.contains('Implant date is required.').scrollIntoView().should('be.visible');

    // 4. Fill out the form with valid test data
    cy.get('#mrn').type(testPatient.mrn);
    cy.get('#firstName').type(testPatient.firstName);
    cy.get('#lastName').type(testPatient.lastName);
    cy.get('#dob').type(testPatient.dob);
    cy.get('#phone').type(testPatient.phone);
    cy.get('#email').type(testPatient.email);
    cy.get('#deviceType').select(testPatient.deviceType);
    cy.get('#manufacturer').type(testPatient.manufacturer);
    cy.get('#model').type(testPatient.model);
    cy.get('#serial').type(testPatient.serial);
    cy.get('#implantDate').type(testPatient.implantDate);

    // 5. Submit
    cy.contains('form button', 'Add Patient').click();

    // 6. Confirm the success message appears
    cy.contains('Patient added successfully.').scrollIntoView().should('be.visible');

    // Modal auto-closes after success
    cy.get('.modal-backdrop', { timeout: 5000 }).should('not.exist');

    // 7. Confirm the new patient appears in the list
    cy.contains('td', testPatient.mrn).should('be.visible');
    cy.contains('td', `${testPatient.firstName} ${testPatient.lastName}`).should('be.visible');
  });
});
