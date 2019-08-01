import React from 'react';
import { TextComponent } from '../components/TextComponent';
import { Connected } from '../assets/ImageConnected';
import { connectTeamsComponent, ITeamsThemeContextProps, TeamsThemeContext } from 'msteams-ui-components-react';
import './Configured.css';

class ConfiguredView extends React.Component<ITeamsThemeContextProps> {
  render() {
    let containerClass = 'configured-container';
    let imageClass = 'configured-image';
    let buttonOrText = <button className='configured-linkbutton' onClick={this.props.gotoShifts}>View schedules & shifts</button>
    if(!this.props.inTeams) {
      containerClass = 'configured-container-mobile';
      imageClass = 'configured-image-mobile';
      buttonOrText = <p>To view schedules and shifts in Teams please navigate to <b>"More Apps"</b> and choose <b>"Shifts"</b>.</p>;
    }

    let content =
      <TextComponent className={this.props.className}>
        <h3>You are connected to <b>{this.props.store}</b> store.</h3>
        {buttonOrText}
        <p>If you experience any problems, please contact your Teams administrator.</p>
      </TextComponent>;

    if (this.props.store.length === 0) {
      content =
        <TextComponent className={this.props.className}>
          This team is already connected to <b>JDA for shifts integration.</b>
          <br/><br/>
          Manage the current configuration if you need to change the connection.
        </TextComponent>;
    }

    return(
      <TeamsThemeContext.Provider value={this.props.context}>
        <div className={containerClass}>
          <div className={imageClass}>
            {Connected()}
          </div>
          {content}
        </div>
      </TeamsThemeContext.Provider>
    );
  }
}

export default ConfiguredView = connectTeamsComponent(ConfiguredView);;
