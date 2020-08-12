import React from "react";

import {createStyles, Grid, LinearProgress, Paper, Theme, Typography} from "@material-ui/core";
import {makeStyles} from "@material-ui/core/styles";

import logo from "../logo.svg"
import "./Logout.scss"

const useStyles = makeStyles((theme: Theme) =>
    createStyles({
        paper: {
            padding: theme.spacing(5),
        },
        progress: {
            marginTop: theme.spacing(1),
        }
    }),
);

const Logout: React.FC = (props) => {
    const classes = useStyles();

    const [success, setSuccess] = React.useState(false);

    // Get parameters and return URL
    let params = new URLSearchParams(window.location.search);
    let returnUrl = params.get("ReturnUrl");

    return (
        <Grid item xs={12} sm={4}>
            <Paper elevation={3} className={classes.paper}>
                <img src={logo} className="logo" alt="logo"/>
                <Typography variant="h5">Sign out</Typography>
                {success && <Typography>Sign out success!</Typography>}
                {!success && <Typography>Hang on a moment while we sign you out.</Typography>}
                {!success && <LinearProgress className={classes.progress}/>}
            </Paper>
        </Grid>
    );
}

export default Logout;
