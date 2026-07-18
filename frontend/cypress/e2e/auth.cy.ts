describe('Authentication flow', () => {
  beforeEach(() => {
    cy.clearCookies();
  });

  it('redirects unauthenticated users to /login, allows login, loads patients, and logs out', () => {
    // 1. Visiting a protected route without a session redirects to /login
    cy.visit('/patients');
    cy.location('pathname', { timeout: 10000 }).should('eq', '/login');

    // 2. Log in with valid credentials
    cy.get('#email').type('doctor@apollo.com');
    cy.get('#password').type('Test@123');
    cy.contains('button', 'Sign in').click();

    // 3. Redirected to /patients and patient data loads
    cy.location('pathname', { timeout: 10000 }).should('eq', '/patients');
    cy.contains('h1', 'Patients').should('be.visible');
    cy.get('tbody tr', { timeout: 10000 }).should('have.length.greaterThan', 0);

    // 4. Log out
    cy.contains('button', 'Logout').click();

    // 5. Redirected back to /login
    cy.location('pathname', { timeout: 10000 }).should('eq', '/login');

    // Confirm the session really is gone - a direct visit to /patients bounces back to /login
    cy.visit('/patients');
    cy.location('pathname', { timeout: 10000 }).should('eq', '/login');
  });
});
