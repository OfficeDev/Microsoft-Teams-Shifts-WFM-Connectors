import React from 'react';
import { getContext, connectTeamsComponent, Input, PrimaryButton, ThemeStyle, TeamsThemeContext, ITeamsThemeContextProps } from 'msteams-ui-components-react';
import { input } from 'msteams-ui-styles-core/lib/components/input';
import ConfiguredView from './views/Configured'
import { TextComponent } from './components/TextComponent'
import * as microsoftTeams from "@microsoft/teams-js";
import './App.css';
import { MSAuthIcon } from './assets/ImageMSAuthIcon';
import { Completed } from './assets/ImageCompleted';


const SUBSCRIBE_PATH = '/subscribe';
const CONFIG_PATH = '/config';

class App extends React.Component<ITeamsThemeContextProps> {
  state = {
    theme: ThemeStyle.Light,
    fontSize: 16,
    storeId: '',
    storeName: '',
    username: '',
    password: '',
    baseURL: '',
    shiftsAppUrl: '',
    AADRedirect: false,
    teamName: undefined,
    teamId: undefined,
    webhookURL: undefined,
    AADAuthCode: undefined,
    complete: false,
    domain: undefined,
    appUrl: undefined,
    teamsConfigured: false,
    serverConfigured: false,
    baseURLFromServer: false,
    initialized: false,
    // error states
    error: undefined,
    authError: undefined,
    storeIdError: undefined,
    usernameError: undefined,
    passwordError: undefined,
    baseURLError: undefined,
    inTeams: false
  };

  pageFontSize = () => {
    let sizeStr = window.getComputedStyle(document.getElementsByTagName('html')[0]).getPropertyValue('font-size');
    sizeStr = sizeStr.replace('px', '');
    let fontSize = parseInt(sizeStr, 10);
    if (!fontSize) {
      fontSize = 16;
    }
    return fontSize;
  }

  getQueryVariable = (variable) => {
    const query = window.location.search.substring(1);
    const vars = query.split('&');
    for (const varPairs of vars) {
      const pair = varPairs.split('=');
      if (decodeURIComponent(pair[0]) === variable) {
        return decodeURIComponent(pair[1]);
      }
    }
    return null;
  }

  updateTheme = (themeStr) => {
    let theme;
    switch (themeStr) {
      case 'dark':
        theme = ThemeStyle.Dark;
        break;
      case 'contrast':
        theme = ThemeStyle.HighContrast;
        break;
      case 'default':
      default:
        theme = ThemeStyle.Light;
    }
    this.setState({ theme });
  }

  handleChange = (event) => {
    this.clearError(event.target.name);
    this.setState({
      [event.target.name]: event.target.value
    });
  }

  clearError = (key) => {
    // Clear username and password errors if errored via server error
    if (key === 'username' || key === 'password' || key === 'baseURL') {
      if (this.state.username) {
        this.setState({ 'usernameError' : undefined });
      }
      if (this.state.password) {
        this.setState({ 'passwordError' : undefined });
      }
      if (this.state.baseURL) {
        this.setState({ 'baseURLError' : undefined });
      }
    }

    this.setState({ [key + 'Error']: undefined });
    // Clear common error
    this.setState({ error: undefined });
  }

  checkValidity = () => {
    let valid = true;
    if (!this.state.AADAuthCode) {
      this.setState({ authError: 'You must approve access to your Team schedules'});
      valid = false;
    }

    if (!this.state.storeId) {
      this.setState({ storeIdError: 'You must provide a store ID'});
      valid = false;
    }

    if (!this.state.username) {
      this.setState({ usernameError: 'You must provide a username'});
      valid = false;
    }

    if (!this.state.password) {
      this.setState({ passwordError: 'You must provide a password'});
      valid = false;
    }

    if (!this.state.baseURL) {
      this.setState({ baseURLError: 'You must provide a base address'});
      valid = false;
    }

    // Check if any inputs are in error from server response
    Object.keys(this.state).forEach(key => {
      if (key.toLowerCase().endsWith('error') && this.state[key] && key !== 'error') {
        valid = false;
      }
    });

    return valid;
  }

  onSubmit = (saveEvent) => {
    if (this.checkValidity()) {
      const body = {
        BaseAddress: this.state.baseURL,
        StoreId: this.state.storeId,
        Username: this.state.username,
        Password: this.state.password,
        AuthorizationCode: this.state.AADAuthCode,
        TeamId: this.state.teamId,
        RedirectUri: this.state.appUrl,
        WebhookUrl: this.state.webhookURL
      };

      fetch(this.state.domain + SUBSCRIBE_PATH, {
        method: 'POST',
        headers: {
          'Content-type': 'application/json; charset=UTF-8'
        },
        body: JSON.stringify(body)
      })
        .then(response => {
          if (response.ok) {
            return response;
          }
          throw response;
        })
        .then(response => response.json())
        .then(data => this.subscribeSuccess(data, saveEvent))
        .catch(error => this.subscribeFailure(error, saveEvent));
    } else {
      saveEvent.notifyFailure();
    }
  };

  subscribeSuccess = (data, saveEvent) => {
    // enable popup save button in Teams
    this.setState({complete: true});
    this.setState({storeId: data.storeId})
    this.setState({storeName: data.storeName})
    microsoftTeams.settings.setSettings({
      entityId: this.state.storeId,
      contentUrl: `${this.state.appUrl}?theme={theme}&storeName=${encodeURIComponent(this.state.storeName)}`,
      removeUrl: `${this.state.appUrl}?remove=true&theme={theme}`,
      websiteUrl: `${this.state.appUrl}?theme=${this.state.theme}&storeName=${encodeURIComponent(this.state.storeName)}`
    });
    microsoftTeams.settings.getSettings((settings) => {
      console.log('Saved Teams settings: ', settings);
    });
    // Timeout to show the success screen
    setTimeout(() => {
      saveEvent.notifySuccess();
    }, 3000);
  };

  subscribeFailure = (response, saveEvent) => {
    switch(response.status) {
      case 401:
        this.setState({ usernameError: 'Username, password or base address invalid' });
        this.setState({ passwordError: 'Username, password or base address invalid' });
        this.setState({ baseURLError: 'Username, password or base address invalid' });
        break;
      case 403:
        this.setState({ authError: 'Microsoft authentication failed, try again' });
        break;
      case 404:
        this.setState({ storeIdError: 'Provided store ID is not found' });
        break;
      case 409:
        this.setState({ error: 'This team is already connected to JDA - remove the exsiting connection before creating a new one' });
        break;
      default:
        this.setState({ error: 'Failed to save configurations, please check your inputs and try again' });
        break;
    }
    this.setState({complete: false});
    this.setState({AADAuthCode: undefined});
    saveEvent.notifyFailure();
  };

  auth = () => {
    const date = new Date();
    const state = this.state.teamId + date.getTime();
    localStorage.setItem('state', state);
    let url = `${this.state.authUrl}?client_id=${this.state.clientId}&response_type=code&response_mode=query&scope=${this.state.authScope}&state=${state}&redirect_uri=${encodeURIComponent(this.state.appUrl)}`;
    console.log('Calling auth url: ', url);
    microsoftTeams.authentication.authenticate({
      successCallback: this.authSuccess,
      failureCallback: this.authFailure,
      url: url
    });
  };

  authSuccess = (authCode) => {
    this.setState({ AADAuthCode: authCode });
    this.clearError('auth');
  };

  authFailure = (response) => {
    // TODO: error handling
    console.log('AD auth failure: ', response);
  };

  // If app is hit from AD auth redirect - get the auth code and pass it to Teams SDK
  checkAuthCode = () => {
    const localState = localStorage.getItem('state');
    localStorage.removeItem('state');
    const queryState = this.getQueryVariable('state');
    const code = this.getQueryVariable('code');
    if (code && localState === queryState) {
      microsoftTeams.authentication.notifySuccess(code);
      return true;
    }
    return false;
  };

  getConfig = (url) => {
    fetch(url, {
      method: 'GET'
    })
      .then(response => {
        if (response.ok) {
          return response.json();
        }
        throw Error(response);
      })
      .then(this.configSuccess)
      .catch(this.configFailure);
  };

  configSuccess = config => {
    console.log('Server config: ', config);
    this.setState({ clientId: config.clientId });
    this.setState({ authScope: encodeURIComponent(config.scope) });
    this.setState({ serverConfigured: config.connected });
    this.setState({ authUrl: config.authorizeUrl})
    if (config.jdaBaseAddress) {
      this.setState({ baseURL: config.jdaBaseAddress });
      this.setState({ baseURLFromServer: true });
    }
    this.setState({ shiftsAppUrl: config.shiftsAppUrl });
    this.setState({ initialized: true });
  };

  configFailure = error => {
    console.log('Error fetching config: ', error);
    this.setState({ initialized: true });
  };

  initializeTeams = (domain) => {
    microsoftTeams.initialize();

    microsoftTeams.getContext(context => {
      console.log('Teams context: ', context);
      if(context) {
        console.log('inTeams: ', true);
        this.setState({ inTeams: true });
        this.setState({ teamId: context.groupId });
        this.setState({ teamName: context.teamName });
        this.getConfig(`${domain}${CONFIG_PATH}/${context.groupId}`);
      }
    });

    microsoftTeams.settings.getSettings((settings) => {
      console.log('Startup Teams settings: ', settings);
      if (settings.entityId) {
        this.setState({ teamsConfigured: true });
      }
      this.setState({ webhookURL: settings.webhookUrl});
    });

    microsoftTeams.registerOnThemeChangeHandler(this.updateTheme);
    microsoftTeams.settings.registerOnSaveHandler(this.onSubmit);
    microsoftTeams.settings.setValidityState(true);
};

  componentDidMount() {
    const domain = `${window.location.protocol}//${window.location.hostname}`;
    this.setState({ appUrl: `${domain}${window.location.pathname}` });
    this.setState({ domain: domain });
    this.initializeTeams(domain);

    const storeName = this.getQueryVariable('storeName');
    if(storeName) {
      console.log('Setting storeName to: ' + storeName);
      this.setState({ storeName: storeName });
      this.setState({ initialized: true });
    } else {
      this.setState({ AADRedirect: this.checkAuthCode() });
    }

    this.updateTheme(this.getQueryVariable('theme'));
    this.setState({
      fontSize: this.pageFontSize(),
    });
  }

  JDAForm(inputThemeClassNames) {
    let baseUrlField;
    if (!this.state.baseURLFromServer) {
      baseUrlField =
      <div>
        <Input name="baseURL" label="Base URL *" errorLabel={this.state.baseURLError} value={this.state.baseURL} onChange={this.handleChange}></Input>
        <br/>
      </div>;
    }

    return (
      <form>
        <br/>
        <TextComponent className="App-text__header">Give your JDA access details for Teams integration</TextComponent>
        <Input name="storeId" label="Store ID *" errorLabel={this.state.storeIdError} value={this.state.storeId} onChange={this.handleChange}></Input>
        <br/>
        <Input name="username" label="JDA Username *" errorLabel={this.state.usernameError} value={this.state.username} onChange={this.handleChange}></Input>
        <br/>
        <Input name="password" type="password" label="JDA Password *" errorLabel={this.state.passwordError} value={this.state.password} onChange={this.handleChange}></Input>
        <br/>
        {baseUrlField}
        <p className={inputThemeClassNames.errorLabel}> {this.state.error} </p>
      </form>
    );
  };

  render() {
    const context = getContext({
      baseFontSize: this.state.fontSize,
      style: this.state.theme
    });
    const inputThemeClassNames = input(context);

    let content;
    if (this.state.complete) {
      content =
        <div className="App-complete">
          <div className="App-complete-image">
            {Completed()}
          </div>
          <div>
            <TextComponent className="App-complete-header">
              Setup complete!
            </TextComponent>
            <TextComponent className="App-complete-text">
              Your shifts will be available to view in the <b>Teams Shifts</b> application shortly.
            </TextComponent>
          </div>
        </div>;
    } else if (this.state.storeName.length > 0 || (this.state.teamsConfigured && this.state.serverConfigured)) {
      content = <ConfiguredView className="App-configured" store={this.state.storeName} inTeams={this.state.inTeams} gotoShifts={this.gotoShifts} context={context}/>;
    } else {
      content = this.JDAForm(inputThemeClassNames);
    }

    let MSAuth;
    if (!this.state.complete && this.state.storeName.length === 0 && !(this.state.teamsConfigured && this.state.serverConfigured)) {
      if (!this.state.AADAuthCode) {
        MSAuth =
          <div className="App-header">
            <TextComponent className="App-text__header">Authenticate to Microsoft to grant JDA access to <i>{this.state.teamName || ''}</i> shifts</TextComponent>
            <button className="App-signIn" onClick={this.auth}>
              {MSAuthIcon(this.state.theme)}
            </button>
            <p className={inputThemeClassNames.errorLabel}> {this.state.authError} </p>
          </div>;
      } else {
        MSAuth =
          <div>
            <TextComponent className="App-text__header">Authenticate to Microsoft to grant JDA access to <i>{this.state.teamName || ''}</i> teams shifts</TextComponent>
            <TextComponent className='App-text'>Microsoft authentication complete</TextComponent>
          </div>;
      }
    }

    if (!this.state.AADRedirect && this.state.initialized) {
      return (
        <TeamsThemeContext.Provider value={context}>
          <div className="App">
            {MSAuth}
            {content}
          </div>
        </TeamsThemeContext.Provider>
      );
    } else {
      return null;
    }
  }

  gotoShifts = (e) => {
    console.log("shiftsAppUrl: ", this.state.shiftsAppUrl);
    microsoftTeams.executeDeepLink(this.state.shiftsAppUrl);
  };

}

export default App = connectTeamsComponent(App);;
