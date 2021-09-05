const jsdom = require('jsdom');
const { JSDOM } = jsdom;
const fs = require('fs');

const BUNDLE_PATH = './dist/bundle.min.js';

const htmlData = fs.readFileSync('./public/index.html');
const jsData = fs.readFileSync(BUNDLE_PATH);
const dom = new JSDOM(htmlData);

const doc = dom.window.document;
const body = dom.window.document.body;
const scriptElement = doc.createElement('script');
const scriptContent = doc.createTextNode(jsData);
scriptElement.appendChild(scriptContent);
body.appendChild(scriptElement);
fs.writeFileSync('./dist/index.html', dom.serialize());
fs.unlinkSync(BUNDLE_PATH);
console.log('Javascript injected to index.html and index.html created in dist, bundle js deleted from dist');
