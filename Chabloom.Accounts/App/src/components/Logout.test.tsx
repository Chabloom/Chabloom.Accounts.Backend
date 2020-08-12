import React from 'react';
import renderer from 'react-test-renderer';
import Logout from "./Logout";

test('Render Logout component', () => {
    renderer.create(<Logout/>);
});
