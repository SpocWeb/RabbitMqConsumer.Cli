# RabbitMqConsumer.Cli
This is a minimal Example for a Windows Service hosting a MassTransit Consumer in .NET Framework 4.8

It uses some extension Methods in XMassTransit.cs to make it easier 
to register all Consumers in a given List of Assemblies. 

This also demonstrates how to Run a generic Host in .NET FW 4.8 

Comments in Program.cs explain how to avoid possible Pitfalls.

To run this you need to
* install RabbitMq  
* fill a Queue with Messages matching RuleEngineCommand.cs or your own Message Class
* configure the RabbitMQ Queue and virtual Host/Path in appSettings.json [as explained here](https://masstransit-project.com/usage/transports/rabbitmq.html#configuration)

