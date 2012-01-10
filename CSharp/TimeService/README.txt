TimeService is an example of a simple Yaler Web service written in C#.

On Windows, make sure that you have at leat .NET 2.0 installed and that your
PATH environment variable includes the framework's directory with, e.g.,

	set PATH=%PATH%;C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727

On Linux, make sure that you have the Mono runtime and the C# compiler.

Now, compile the example source with

	csc TimeService.cs

or on Mono with

	gmcs TimeService.cs

Then create a free Yaler account at http://yaler.net/ to get a unique relay
domain for the Yaler trial instance hosted at try.yaler.net.

To start TimeService on your computer enter

	TimeService try.yaler.net <relay domain>

or on Mono

	mono TimeService try.yaler.net <relay domain>

E.g., for the relay domain gsiot-ffmq-ttd5 type

	TimeService try.yaler.net gsiot-ffmq-ttd5

To access the service from everywhere, visit

	http://try.yaler.net/<relay domain>

In our example, TimeService would be accessible at

	http://try.yaler.net/gsiot-ffmq-ttd5

If everything works fine, you should see a static Web page with the actual time
on each reload, served right from your computer.