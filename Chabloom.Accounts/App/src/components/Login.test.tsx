import React from 'react';
import renderer from 'react-test-renderer';
import Login from "./Login";

test('Render Login component', () => {
    renderer.create(<Login/>);
});
