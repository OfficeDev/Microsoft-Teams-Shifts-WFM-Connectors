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
    wfmBuId: '',
    wfmBuName: '',
    shiftsAppUrl: '',
    AADRedirect: false,
    teamName: undefined,
    teamId: undefined,
    AADAuthCode: undefined,
    complete: false,
    domain: undefined,
    appUrl: undefined,
    teamsConfigured: false,
    serverConfigured: false,
    initialized: false,
    // error states
    error: undefined,
    authError: undefined,
    wfmBuIdError: undefined,
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

    if (!this.state.wfmBuId) {
      this.setState({ wfmBuIdError: 'You must provide a business unit ID'});
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
        WfmBuId: this.state.wfmBuId,
        AuthorizationCode: this.state.AADAuthCode,
        TeamId: this.state.teamId,
        RedirectUri: this.state.appUrl
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
    this.setState({wfmBuId: data.wfmBuId})
    this.setState({wfmBuName: data.wfmBuName})
    microsoftTeams.settings.setSettings({
      entityId: this.state.wfmBuId,
      contentUrl: `${this.state.appUrl}?theme={theme}&wfmBuName=${encodeURIComponent(this.state.wfmBuName)}`,
      removeUrl: `${this.state.appUrl}?remove=true&theme={theme}`,
      websiteUrl: `${this.state.appUrl}?theme=${this.state.theme}&wfmBuName=${encodeURIComponent(this.state.wfmBuName)}`
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
      case 400:
        this.setState({ error: 'Invalid business unit Id.' });
        break;
      case 403:
        this.setState({ authError: 'Microsoft authentication failed! Try again' });
        break;
      case 404:
        this.setState({ wfmBuIdError: 'Provided business unit ID is not found!' });
        break;
      default:
        this.setState({ error: 'Failed to connect the team, please check your inputs and try again.' });
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

    const wfmBuName = this.getQueryVariable('wfmBuName');
    if(wfmBuName) {
      console.log('Setting wfmBuName to: ' + wfmBuName);
      this.setState({ wfmBuName: wfmBuName });
      this.setState({ initialized: true });
    } else {
      this.setState({ AADRedirect: this.checkAuthCode() });
    }

    this.updateTheme(this.getQueryVariable('theme'));
    this.setState({
      fontSize: this.pageFontSize(),
    });
  }

  WFMForm(inputThemeClassNames) {
    return (
      <form>
        <br/>
        <TextComponent className="App-text__header">Enter the ID of the WFM business unit this team should be connected to:</TextComponent>
        <Input name="wfmBuId" label="Business Unit ID *" errorLabel={this.state.wfmBuIdError} value={this.state.wfmBuId} onChange={this.handleChange}></Input>
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
    } else if (this.state.wfmBuName.length > 0 || (this.state.teamsConfigured && this.state.serverConfigured)) {
      content = <ConfiguredView className="App-configured" wfmBuName={this.state.wfmBuName} inTeams={this.state.inTeams} gotoShifts={this.gotoShifts} context={context}/>;
    } else {
      content = this.WFMForm(inputThemeClassNames);
    }

    let MSAuth;
    if (!this.state.complete && this.state.wfmBuName.length === 0 && !(this.state.teamsConfigured && this.state.serverConfigured)) {
      if (!this.state.AADAuthCode) {
        MSAuth =
          <div className="App-header">
            <TextComponent className="App-text__header">Authenticate to Microsoft to grant access to <i>{this.state.teamName || ''}</i> shifts</TextComponent>
            <button className="App-signIn" onClick={this.auth}>
              {MSAuthIcon(this.state.theme)}
            </button>
            <p className={inputThemeClassNames.errorLabel}> {this.state.authError} </p>
          </div>;
      } else {
        MSAuth =
          <div>
            <TextComponent className="App-text__header">Authenticate to Microsoft to grant access to <i>{this.state.teamName || ''}</i> teams shifts</TextComponent>
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
