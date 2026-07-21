describe('A.3: Advanced Search filters and the clinic-wide Transmission Schedule page', () => {
  const uniqueSuffix = Date.now();
  const testPatient = {
    mrn: `E2E-${uniqueSuffix}`,
    firstName: 'AdvSearch',
    lastName: `Unique${uniqueSuffix}`,
    dob: '1970-01-01',
    deviceType: 'Pacemaker',
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

  it('filters the patient list via Advanced Search and resets it with Clear Filters', () => {
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

    cy.get('tbody tr', { timeout: 10000 }).should('have.length.greaterThan', 1);

    cy.get('.advanced-search__toggle').click();
    cy.get('.advanced-search__form').should('be.visible');
    cy.get('#searchKeyword').type(testPatient.lastName);
    cy.contains('.advanced-search__form button', 'Search').click();

    cy.get('tbody tr', { timeout: 10000 }).should('have.length', 1);
    cy.contains('.patient-name', `${testPatient.firstName} ${testPatient.lastName}`).should('be.visible');

    cy.contains('.advanced-search__actions button', 'Clear Filters').click();
    cy.get('#searchKeyword').should('have.value', '');
    cy.get('tbody tr', { timeout: 10000 }).should('have.length.greaterThan', 1);
  });

  it('filters by device type and active status', () => {
    const icdPatient = {
      mrn: `E2E-${uniqueSuffix}D`,
      firstName: 'DeviceFilter',
      lastName: `Patient${uniqueSuffix}`,
      dob: '1970-01-01',
      deviceType: 'ICD',
      serial: `E2E-ICD-SN-${uniqueSuffix}`,
      implantDate: '2023-01-10'
    };

    cy.contains('a', 'Patients').click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/patients');

    cy.contains('button', '+ Add Patient').click();
    cy.get('.modal-panel').should('be.visible');
    cy.get('#mrn').type(icdPatient.mrn);
    cy.get('#firstName').type(icdPatient.firstName);
    cy.get('#lastName').type(icdPatient.lastName);
    cy.get('#dob').type(icdPatient.dob);
    cy.get('#deviceType').select(icdPatient.deviceType);
    cy.get('#serial').type(icdPatient.serial);
    cy.get('#implantDate').type(icdPatient.implantDate);
    cy.contains('form button', 'Add Patient').click();
    cy.contains('Patient added successfully.').scrollIntoView().should('be.visible');
    cy.get('.modal-backdrop', { timeout: 5000 }).should('not.exist');

    cy.get('.advanced-search__toggle').click();
    cy.get('#searchDeviceType').select('ICD');
    cy.get('#searchStatus').select('Active');
    cy.contains('.advanced-search__form button', 'Search').click();

    cy.get('tbody tr', { timeout: 10000 }).should('have.length.greaterThan', 0);
    cy.contains('.patient-name', `${icdPatient.firstName} ${icdPatient.lastName}`).should('be.visible');
    // .should() with a callback retries until the filtered response has
    // re-rendered the table - a plain .then() snapshots the stale, unfiltered
    // rows and races the in-flight search request.
    cy.get('tbody', { timeout: 10000 }).should(($tbody) => {
      const rowTexts = $tbody.find('tr').toArray().map((row) => row.textContent ?? '');
      expect(rowTexts.length).to.be.greaterThan(0);
      rowTexts.forEach((text) => expect(text).to.include('ICD'));
    });
  });

  it('loads the clinic-wide Transmission Schedule page and toggles the Next Scheduled sort order', () => {
    cy.contains('a', 'Transmission Schedule').click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/transmission-schedule');
    cy.contains('h1', 'Transmission Schedule').should('be.visible');

    cy.get('tbody tr', { timeout: 10000 }).should('have.length.greaterThan', 0);
    cy.get('.sortable-header .sort-indicator').should('contain.text', '▲');

    cy.get('tbody tr').its('length').then((initialCount) => {
      cy.get('.sortable-header').click();
      cy.get('.sortable-header .sort-indicator').should('contain.text', '▼');
      cy.get('tbody tr').should('have.length', initialCount);

      cy.get('.sortable-header').click();
      cy.get('.sortable-header .sort-indicator').should('contain.text', '▲');
      cy.get('tbody tr').should('have.length', initialCount);
    });
  });
});
