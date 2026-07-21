describe('Sprint 8: Clinic Management (Admin-only user accounts)', () => {
  const uniqueSuffix = Date.now();
  const newUser = {
    firstName: 'Ravi',
    lastName: `Test${uniqueSuffix}`,
    email: `ravi.test${uniqueSuffix}@apollo.com`
  };

  function loginAs(email: string, password: string): void {
    cy.clearCookies();
    localStorage.clear();
    cy.visit('/login');
    cy.get('#email').type(email);
    cy.get('#password').type(password);
    cy.contains('button', 'Sign in').click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/dashboard');
  }

  it('lets an Admin create a clinic user, shows the one-time temp password, and suspends the user', () => {
    loginAs('admin@apollo.com', 'Admin@123');

    cy.contains('a', 'Clinic Management').should('be.visible').click();
    cy.location('pathname', { timeout: 10000 }).should('eq', '/clinic-management');

    cy.contains('button', '+ New User').click();
    cy.get('#firstName').type(newUser.firstName);
    cy.get('#lastName').type(newUser.lastName);
    cy.get('#email').type(newUser.email);
    cy.contains('form button', 'Create User').click();

    // One-time temporary password banner.
    cy.contains('Temporary password created', { timeout: 10000 }).should('be.visible');
    cy.contains("won't be shown again").should('be.visible');
    cy.get('.temp-password')
      .invoke('text')
      .should('match', /^\S{12}$/);

    // New user appears in the table.
    cy.contains('tr', newUser.email, { timeout: 10000 }).within(() => {
      cy.contains(`${newUser.firstName} ${newUser.lastName}`).should('be.visible');
      cy.contains('.status-badge', 'Active').should('be.visible');
    });

    // Suspend the new user and confirm the status flips.
    cy.contains('tr', newUser.email).within(() => {
      cy.contains('button', 'Suspend').click();
    });
    cy.contains('tr', newUser.email, { timeout: 10000 }).within(() => {
      cy.contains('.status-badge', 'Suspended').should('be.visible');
      cy.contains('button', 'Activate').should('be.visible');
    });

    // Confirm the suspension was actually persisted server-side.
    cy.reload();
    cy.contains('tr', newUser.email, { timeout: 10000 }).within(() => {
      cy.contains('.status-badge', 'Suspended').should('be.visible');
    });
  });

  it('does not show the Clinic Management nav link to a regular Clinician', () => {
    loginAs('doctor@apollo.com', 'Test@123');

    cy.contains('a', 'Dashboard').should('be.visible');
    cy.contains('a', 'Clinic Management').should('not.exist');

    // Route guard blocks direct navigation too, not just the hidden link.
    cy.visit('/clinic-management');
    cy.location('pathname', { timeout: 10000 }).should('eq', '/dashboard');
  });
});
