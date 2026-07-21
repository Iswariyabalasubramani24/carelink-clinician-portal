describe('Platform: SuperAdmin hospital onboarding', () => {
  const uniqueSuffix = Date.now();
  const newHospital = {
    name: `E2E Hospital ${uniqueSuffix}`,
    region: `Testland${uniqueSuffix}`,
    adminFirstName: 'Onboard',
    adminLastName: `Admin${uniqueSuffix}`,
    adminEmail: `admin${uniqueSuffix}@e2ehospital.test`
  };

  function loginAs(email: string, password: string): void {
    cy.clearCookies();
    localStorage.clear();
    cy.visit('/login');
    cy.get('#email').type(email);
    cy.get('#password').type(password);
    cy.contains('button', 'Sign in').click();
  }

  it('hides the Hospitals page from a regular hospital Admin (nav link and route guard)', () => {
    loginAs('admin@apollo.com', 'Admin@123');
    cy.location('pathname', { timeout: 10000 }).should('eq', '/dashboard');

    cy.contains('a', 'Hospitals').should('not.exist');

    cy.visit('/hospitals');
    cy.location('pathname', { timeout: 10000 }).should('eq', '/dashboard');
  });

  it('lands the SuperAdmin on the Hospitals page with a platform-only nav', () => {
    loginAs('platform@carelink.local', 'Platform@123');

    // SuperAdmins land on hospital management, not the clinical dashboard.
    cy.location('pathname', { timeout: 10000 }).should('eq', '/hospitals');
    cy.contains('h1', 'Hospitals').should('be.visible');

    // Clinical nav is hidden for the platform operator.
    cy.contains('a', 'Patients').should('not.exist');
    cy.contains('a', 'Dashboard').should('not.exist');
    cy.contains('a', 'Hospitals').should('be.visible');

    // The seeded demo hospitals are listed.
    cy.get('tbody tr', { timeout: 10000 }).should('have.length.greaterThan', 0);
    cy.contains('.hospital-name', 'Apollo Hospital').should('be.visible');
  });

  it('provisions a new hospital, reveals the one-time temp password, and the new admin can log in and manage their clinic', () => {
    loginAs('platform@carelink.local', 'Platform@123');
    cy.location('pathname', { timeout: 10000 }).should('eq', '/hospitals');

    cy.contains('button', '+ New Hospital').click();
    cy.get('.modal-panel').should('be.visible');
    cy.get('#hospitalName').type(newHospital.name);
    cy.get('#hospitalRegion').type(newHospital.region);
    cy.get('#adminFirstName').type(newHospital.adminFirstName);
    cy.get('#adminLastName').type(newHospital.adminLastName);
    cy.get('#adminEmail').type(newHospital.adminEmail);
    cy.contains('form button', 'Provision Hospital').click();

    // One-time temporary password banner for the new hospital's first admin.
    cy.contains('Hospital provisioned', { timeout: 10000 }).should('be.visible');
    cy.contains(newHospital.adminEmail).should('be.visible');
    cy.get('.temp-password')
      .invoke('text')
      .then((tempPassword) => {
        expect(tempPassword.trim()).to.match(/^\S{12}$/);

        // New hospital appears in the management list.
        cy.contains('tr', newHospital.name, { timeout: 10000 }).within(() => {
          cy.contains('.status-badge', 'Active').should('be.visible');
        });

        // The freshly minted admin can log in with the one-time password and
        // sees their own (empty) hospital: Clinic Management works, no other
        // hospital's data leaks in.
        cy.contains('button', 'Logout').click();
        cy.location('pathname', { timeout: 10000 }).should('eq', '/login');

        loginAs(newHospital.adminEmail, tempPassword.trim());
        cy.location('pathname', { timeout: 10000 }).should('eq', '/dashboard');

        cy.contains('a', 'Clinic Management').click();
        cy.location('pathname', { timeout: 10000 }).should('eq', '/clinic-management');
        cy.contains('tr', newHospital.adminEmail, { timeout: 10000 }).should('be.visible');
        cy.get('tbody tr').should('have.length', 1);

        cy.contains('a', 'Patients').click();
        cy.location('pathname', { timeout: 10000 }).should('eq', '/patients');
        cy.contains('No patients yet', { timeout: 10000 }).should('be.visible');
      });
  });
});
