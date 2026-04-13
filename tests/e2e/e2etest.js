import { Selector, ClientFunction } from 'testcafe';

const getPathname = ClientFunction(() => window.location.pathname);

fixture`Login and chat flow`
    .page`http://localhost:5173`;

test('User can login and see chat rooms', async t => {
    const emailInput = Selector('input[type="email"]');
    const passwordInput = Selector('input[type="password"]');
    const loginButton = Selector('button[type="submit"]');
    const roomDirectoryHeading = Selector('h1').withText('Room Directory');
    const roomListContainer = Selector('div.grid');

    await t.navigateTo('http://localhost:5173/login');

    await t
        .expect(emailInput.exists).ok({ timeout: 10000 })
        .expect(passwordInput.exists).ok({ timeout: 10000 });

    await t
        .typeText(emailInput, 'admin@gmail.com')
        .typeText(passwordInput, 'adminadmin')
        .click(loginButton);

    // Wait for the Room Directory heading — Selector-based waiting is more
    // reliable than ClientFunction for TestCafe's smart-assertion retries
    // through a React Router navigation + Shell auth-guard re-render cycle.
    await t.expect(roomDirectoryHeading.exists).ok({ timeout: 20000 });

    await t.expect(getPathname()).contains('/rooms', { timeout: 5000 });
    await t.expect(roomDirectoryHeading.visible).ok({ timeout: 10000 });
    await t.expect(roomListContainer.visible).ok({ timeout: 10000 });
})