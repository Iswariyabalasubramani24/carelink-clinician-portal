describe('Language switching', () => {
  beforeEach(() => {
    // The header's language switcher is hidden on /login (which has its own
    // country/language selector, Sprint 3 Part B), so this exercises it on
    // the patients page instead, which requires logging in first.
    cy.clearCookies();
    localStorage.clear();
    cy.visit('/login');
    cy.get('#email').type('doctor@apollo.com');
    cy.get('#password').type('Test@123');
    cy.contains('button', 'Sign in').click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/dashboard');

    // Navigate to the patients page, where this test's assertions live.
    cy.contains('a', 'Patients').click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/patients');
  });

  it('switches the UI to French and back to English via the header language switcher', () => {
    // Starts in English
    cy.contains('+ Add Patient').should('be.visible');

    // Switch to French
    cy.get('select.language-switcher').select('fr');

    cy.contains('+ Ajouter un patient').should('be.visible');
    cy.contains('+ Add Patient').should('not.exist');

    // Switch back to English
    cy.get('select.language-switcher').select('en');

    cy.contains('+ Add Patient').should('be.visible');
    cy.contains('+ Ajouter un patient').should('not.exist');
  });
});
