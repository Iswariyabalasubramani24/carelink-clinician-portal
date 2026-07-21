describe('Sprint 6 + 7: Report generation/download and Comments and Notes', () => {
  const uniqueSuffix = Date.now();
  const testPatient = {
    mrn: `E2E-${uniqueSuffix}`,
    firstName: 'ReportsNotes',
    lastName: `Patient${uniqueSuffix}`,
    dob: '1970-01-01',
    deviceType: 'ICD',
    serial: `E2E-SN-${uniqueSuffix}`,
    implantDate: '2023-01-10'
  };
  const noteContent = `Patient responding well to treatment - E2E note ${uniqueSuffix}`;

  beforeEach(() => {
    cy.clearCookies();
    localStorage.clear();
    cy.visit('/login');
    cy.get('#email').type('doctor@apollo.com');
    cy.get('#password').type('Test@123');
    cy.contains('button', 'Sign in').click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/dashboard');
  });

  it('logs in, generates and downloads a report for a patient, then posts a Comments and Notes entry and confirms it in the list', () => {
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

    // --- Sprint 6 regression check: generate and download a report ---
    cy.contains('button', 'Reports').click();
    cy.contains('No reports generated yet.', { timeout: 10000 }).should('be.visible');

    cy.contains('button', 'Generate Report').click();
    cy.contains('td', 'Full Report', { timeout: 10000 }).should('be.visible');

    cy.intercept('GET', '**/api/v1/reports/*/download').as('downloadReport');
    cy.contains('button', 'Download').click();
    cy.wait('@downloadReport').its('response').then((response) => {
      expect(response?.statusCode).to.eq(200);
      expect(response?.headers['content-type']).to.include('application/pdf');
    });

    // --- Sprint 7: Comments and Notes ---
    cy.contains('button', 'Comments and Notes').click();
    cy.contains('No notes yet.', { timeout: 10000 }).should('be.visible');

    cy.get('.note-textarea').type(noteContent);
    cy.contains('.note-composer__actions button', 'Post').click();

    cy.contains('.note-item', noteContent, { timeout: 10000 }).within(() => {
      cy.contains('Anita Rao').should('be.visible');
      cy.contains(noteContent).should('be.visible');
    });

    // Confirm the note was actually persisted server-side, not just local UI state.
    cy.reload();
    cy.contains('button', 'Comments and Notes').click();
    cy.contains('.note-item', noteContent, { timeout: 10000 }).within(() => {
      cy.contains('Anita Rao').should('be.visible');
    });
  });
});
