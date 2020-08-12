import React from 'react';

import {BrowserRouter as Router, Route, Switch} from "react-router-dom";

import Login from "./components/Login";
import Logout from "./components/Logout";
import {Grid} from "@material-ui/core";

import './App.scss';

const App: React.FC = () => {
    return (
        <Router>
            <Grid container alignItems="center" justify="center" style={{ minHeight: '100vh' }}>
                <Switch>
                    <Route path="/login">
                        <Login/>
                    </Route>
                    <Route path="/logout">
                        <Logout/>
                    </Route>
                </Switch>
            </Grid>
        </Router>
    );
}

export default App;
