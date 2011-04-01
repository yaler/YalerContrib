TimeService is an example of a simple Yaler Web service written in C#.

Download the Yaler source from http://yaler.org/ and build it as described in
README.txt. Then start the Yaler relay server with

	java -ea -cp yaler.jar org.yaler.Yaler 127.0.0.1:80

On Windows, make sure that you have at leat .NET 2.0 installed and that your
PATH environment variable includes the framework's directory with, e.g.,

	set PATH=%PATH%;C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727

On Linux, make sure that you have the Mono runtime and the C# compiler.

Now, compile the example source with

	csc TimeService.cs

or on Mono with

	gmcs TimeService.cs

Start the service on your computer with

	TimeService 127.0.0.1 my-computer

or on Mono with

	mono TimeService.exe 127.0.0.1 my-computer

To access the service on your computer, visit

	http://127.0.0.1:80/my-computer/

with your browser. If it works, you should see a static Web page with the actual
time on each reload. To get the full Yaler experience (access from any browser),
you'll have to host the relay server on a separate computer with a public IP
address and adapt all occurrences of 127.0.0.1 above and in the example source.
