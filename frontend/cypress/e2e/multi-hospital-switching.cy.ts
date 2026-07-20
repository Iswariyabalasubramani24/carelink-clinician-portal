describe('Multi-hospital clinic switching', () => {
  beforeEach(() => {
    cy.clearCookies();
    localStorage.clear();
    cy.visit('/login');
    cy.get('#email').type('doctor@apollo.com');
    cy.get('#password').type('Test@123');
    cy.contains('button', 'Sign in').click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/patients');
  });

  it('logs in on Apollo Hospital data, switches to Charite via the header, and shows only Charite patients', () => {
    // Anita Rao's default tenant is Apollo Hospital - lands there on login
    cy.get('.clinic-switcher__current').should('contain.text', 'Apollo Hospital');
    cy.contains('.patient-name', 'Rajesh Kumar', { timeout: 10000 }).should('be.visible');
    cy.contains('.patient-name', 'Priya Sharma').should('be.visible');
    cy.contains('.patient-name', 'Anil Verma').should('be.visible');
    cy.contains('.patient-name', 'Sunita Iyer').should('be.visible');

    // Switch to Charite Hospital via the header clinic switcher
    cy.get('select.clinic-switcher__select').select('Charite Hospital');

    // Header now reflects Charite as the active hospital
    cy.get('.clinic-switcher__current', { timeout: 10000 }).should('contain.text', 'Charite Hospital');

    // Patient list now shows ONLY Charite's patient - proving tenant isolation holds after switching
    cy.get('tbody tr', { timeout: 10000 }).should('have.length', 1);
    cy.contains('.patient-name', 'Klaus Weber').should('be.visible');
  });
});
