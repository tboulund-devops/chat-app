import { Selector } from 'testcafe';

const API_URL = 'http://localhost:5285';

fixture`Login and chat flow`
    .page`http://localhost:5173`
    .before(async () => {
        // Seed the test user via the API before tests run
        const response = await fetch(`${API_URL}/api/auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                firstName: 'Admin',
                lastName: 'User',
                email: 'admin@gmail.com',
                password: 'adminadmin',
                role: 0
            })
        });
        if (!response.ok && response.status !== 409) {
            throw new Error(`Failed to seed test user: ${response.status}`);
        }
    });

test('User can login and see chat rooms', async t => {
    const emailInput = Selector('input[type="email"]');
    const passwordInput = Selector('input[type="password"]');
    const loginButton = Selector('button[type="submit"]');
    const roomDirectoryHeading = Selector('h1').withText('Room Directory');
    const roomListContainer = Selector('div.grid');

    await t.navigateTo('http://localhost:5173/login');

    await t
        .expect(emailInput.exists)
        .ok()
        .expect(passwordInput.exists)
        .ok()
        .typeText(emailInput, 'admin@gmail.com')
        .typeText(passwordInput, 'adminadmin')
        .click(loginButton)
        .expect(t.eval(() => window.location.pathname))
        .contains('/rooms')
        .expect(roomDirectoryHeading.visible)
        .ok()
        .expect(roomListContainer.visible)
        .ok();
})