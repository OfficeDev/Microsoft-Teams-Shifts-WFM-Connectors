import React from 'react';
import { connectTeamsComponent, ThemeStyle } from 'msteams-ui-components-react';

const TextComponentInternal: React.FunctionComponent = (props) => {
  const { context, ...rest } = props;
  const { colors, style } = context;

  const styleProps = {};
  switch(style) {
    case ThemeStyle.Dark:
      styleProps.color = colors.dark.white;
      break;
    case ThemeStyle.HighContrast:
      styleProps.color = colors.highContrast.white;
      break;
    case ThemeStyle.Light:
      styleProps.color = colors.light.black;
      break;
    default:
      styleProps.color = colors.light.black;
  }

  return <p style={styleProps} {...rest}>{props.children}</p>;
};

export const TextComponent = connectTeamsComponent(TextComponentInternal);
