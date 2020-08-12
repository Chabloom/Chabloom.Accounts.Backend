import React from "react";

import {
    Button,
    createStyles,
    FormGroup,
    Grid,
    LinearProgress,
    Paper,
    TextField,
    Theme,
    Typography
} from "@material-ui/core";
import {Alert, AlertTitle} from '@material-ui/lab';
import {makeStyles} from "@material-ui/core/styles";

import logo from "../logo.svg"
import "./Login.scss"

const useStyles = makeStyles((theme: Theme) =>
    createStyles({
        paper: {
            padding: theme.spacing(5),
        },
        mt1: {
            marginTop: theme.spacing(1),
        }
    }),
);

const Login: React.FC = () => {
    // Initialize classes
    const classes = useStyles();

    // Initialize state variables
    const [email, setEmail] = React.useState("");
    const [password, setPassword] = React.useState("");
    const [error, setError] = React.useState("");
    const [processing, setProcessing] = React.useState(false);

    // Get parameters and return URL
    let params = new URLSearchParams(window.location.search);
    let returnUrl = params.get("ReturnUrl");

    return (
        <Grid item xs={12} sm={4}>
            <Paper className={classes.paper} elevation={3}>
                <img src={logo} className="logo" alt="logo"/>
                <Typography variant="h5">Sign in</Typography>
                <form onSubmit={e => e.preventDefault()}>
                    <FormGroup>
                        <TextField required name="email" label="Email" value={email} type="email"
                                   disabled={processing} onChange={e => setEmail(e.target.value)}/>
                        <TextField required name="password" label="Password" value={password} type="password"
                                   disabled={processing} onChange={e => setPassword(e.target.value)}/>
                    </FormGroup>
                    {error &&
                    <Alert className={classes.mt1} severity="error">
                        <AlertTitle>Error</AlertTitle>
                        {error}
                    </Alert>
                    }
                    {processing &&
                    <LinearProgress className={classes.mt1}/>
                    }
                    <Button
                        className={classes.mt1}
                        variant="contained"
                        color="primary"
                        type="submit"
                        disabled={processing}
                        onClick={() => {
                            setError("");
                            setProcessing(true);
                            const data = {
                                "email": email,
                                "password": password
                            };
                            fetch("https://localhost:44303/api/authentication/login", {
                                method: "POST",
                                headers: {
                                    'Content-Type': 'application/json'
                                },
                                body: JSON.stringify(data)
                            }).then(value => {
                                if (value.status === 401) {
                                    setError("Invalid username or password.");
                                } else if (value.status === 200 && returnUrl) {
                                    window.location.replace(returnUrl);
                                }
                            }).catch(reason => {
                                setError(reason.message);
                            }).finally(() => setProcessing(false));
                        }}>
                        Login
                    </Button>
                </form>
            </Paper>
        </Grid>
    );
}

export default Login;
