import { Selector } from 'testcafe';

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