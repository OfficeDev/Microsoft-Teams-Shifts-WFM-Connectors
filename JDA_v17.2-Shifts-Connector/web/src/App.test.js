import React from 'react';
import { shallow } from 'enzyme';
import sinon from 'sinon';
import App from './App';


describe('app rendering', () => {
  let component;

  beforeEach(() => {
    component = shallow(<App />);
  });

  it('renders without crashing', () => {
    expect(component).not.toBe(undefined);
  });

});
