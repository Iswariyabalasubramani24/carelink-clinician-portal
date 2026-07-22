describe('Change password: temp-password holder rotates their own credential', () => {
  const uniqueSuffix = Date.now();
  const newUser = {
    firstName: 'PwRotate',
    lastName: `Test${uniqueSuffix}`,
    email: `pw.rotate${uniqueSuffix}@apollo.com`
  };
  const newPassword = `Rotated#Pw${uniqueSuffix}`;

  function login(email: string, password: string): void {
    cy.clearCookies();
    localStorage.clear();
    cy.visit('/login');
    cy.get('#email').type(email);
    cy.get('#password').type(password);
    cy.contains('button', 'Sign in').click();
  }

  it('admin creates a user; the user signs in with the temp password, changes it, and the old one stops working', () => {
    // --- Admin provisions the account and reveals the one-time temp password ---
    login('admin@apollo.com', 'Admin@123');
    cy.location('pathname', { timeout: 10000 }).should('eq', '/dashboard');

    cy.contains('a', 'Clinic Management').click();
    cy.contains('button', '+ New User').click();
    cy.get('#firstName').type(newUser.firstName);
    cy.get('#lastName').type(newUser.lastName);
    cy.get('#email').type(newUser.email);
    cy.contains('form button', 'Create User').click();
    cy.contains('Temporary password created', { timeout: 10000 }).should('be.visible');

    cy.get('.temp-password')
      .invoke('text')
      .then((tempPassword) => {
        const tempPw = tempPassword.trim();

        // --- The new user signs in with the temp password and rotates it ---
        cy.contains('button', 'Logout').click();
        cy.location('pathname', { timeout: 10000 }).should('eq', '/login');

        login(newUser.email, tempPw);
        cy.location('pathname', { timeout: 10000 }).should('eq', '/dashboard');

        // The header profile links to the change-password page.
        cy.get('.app-header__profile').click();
        cy.location('pathname', { timeout: 10000 }).should('eq', '/change-password');

        cy.get('#currentPassword').type(tempPw);
        cy.get('#newPassword').type(newPassword);
        cy.get('#confirmPassword').type(newPassword);
        cy.contains('button', 'Change password').click();
        cy.contains('Your password has been changed.', { timeout: 10000 }).should('be.visible');

        // --- The temp password is now dead; the new one works ---
        cy.contains('button', 'Logout').click();
        cy.location('pathname', { timeout: 10000 }).should('eq', '/login');

        login(newUser.email, tempPw);
        cy.contains('Invalid email or password.', { timeout: 10000 }).should('be.visible');

        login(newUser.email, newPassword);
        cy.location('pathname', { timeout: 10000 }).should('eq', '/dashboard');
      });
  });
});
