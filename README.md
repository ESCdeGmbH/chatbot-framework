# Chatbot Framework
[![Build Status](https://dev.azure.com/ESCde/chatbot-framework/_apis/build/status/chatbot-framework-ASP.NET%20Core-CI?branchName=master)](https://dev.azure.com/ESCde/chatbot-framework/_build/latest?definitionId=25&branchName=master)

This framework based on [Microsofts Bot Framework](https://dev.botframework.com/) provides an easy applicable and dynamically building dialogue management. 

For classification of user input various classifier can be taken. In our example we've chosen [Microsofts Luis](https://luis.ai/). We recommend to create several apps for classification of: 
1) Topics (dependent on purpose of the bot)
2) Question types
3) Fixed commands (e.g. "cancel")

Dependent on the classification results the dialogues are chosen.
The bot framework offers two kinds of dialogues to react on inputs.
The single step dialogue selects a random response out of a set of answers stored in a Json file.
The multi step dialogue offers the possibility to build the dialogue with counterquestions dynamically during runtime.
Meanwhile the context is stored in the bot state.

The framework can be used is language independent and supports a lot of different messaging endpoints. 

**You don't have to write much code to create your first chatbot. Try it out!**

To learn more about it, take a look in our [Wiki](https://github.com/ESCdeGmbH/chatbot-framework/wiki).