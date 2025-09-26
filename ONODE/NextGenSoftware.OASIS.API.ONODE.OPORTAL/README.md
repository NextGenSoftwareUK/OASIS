This is a work in progress. Please feel free to submit any information you feel is relevant, as this is just a temporary way to lay out the project structure so we're all on the same page.

To be added (feel free to submit content suggestions):

global .gitignore and .editorconfig files

Style Guide is in the beginning stages, again accepting suggestions for the code styles. All of this will be added to upcoming edits to the project documentation as well as an onboarding manual.alert

Going forward, please make sure you are working in the appropriate directory! 



Directory Structure (main directory for each framework has been highlighted)
<pre>
├───.config
├───api
├───App_Start
├───ClientApp   // Angular Project Root
│   ├───oasis-angular
│   │   └───oasis-web
│   │       ├───dist
│   │       │   └───oasis-web
│   │       │       └───assets
│   │       │           ├───css
│   │       │           │   ├───base
│   │       │           │   ├───components
│   │       │           │   └───custom-style
│   │       │           ├───globalStyles
│   │       │           │   ├───base
│   │       │           │   ├───components
│   │       │           │   └───custom-style
│   │       │           └───img
│   │       └───src
│   │           ├───app
│   │           │   ├───common
│   │           │   │   └───modal
│   │           │   ├───components
│   │           │   │   ├───header
│   │           │   │   ├───home
│   │           │   │   ├───login
│   │           │   │   ├───side-nav
│   │           │   │   └───signup
│   │           │   └───services
│   │           ├───assets
│   │           │   ├───css
│   │           │   │   ├───base
│   │           │   │   ├───components
│   │           │   │   └───custom-style
│   │           │   ├───globalStyles
│   │           │   │   ├───base
│   │           │   │   ├───components
│   │           │   │   └───custom-style
│   │           │   └───img
│   │           └───environments
│   ├───oasis-pure-js
│   │   └───dist
│   │       └───oasis-web
│   │           └───assets
│   │               ├───css
│   │               │   ├───base
│   │               │   ├───components
│   │               │   └───custom-style
│   │               ├───globalStyles
│   │               │   ├───base
│   │               │   ├───components
│   │               │   └───custom-style
│   │               └───img
│   └───oasis-web
├───Controllers
├───Pages
├───Properties
├───<b>react-app</b>     //  React Project Root
│   ├───public
│   └───src
│       ├───Components
│       ├───CSS
│       └───img
└───<b>wwwroot</b>     //   Web/HTML Project Root
    ├───assets
    │   ├───css
    │   ├───img
    │   └───js
    ├───postman
    └───source
        ├───.vscode
        ├───dist
        │   └───assets
        │       ├───css
        │       ├───img
        │       └───js
        ├───main
        │   └───assets
        │       ├───css
        │       │   ├───base
        │       │   ├───components
        │       │   └───custom-style
        │       ├───img
        │       └───js
        │           └───components
        └───settings
<code>