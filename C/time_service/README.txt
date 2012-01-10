time_service is an example of a simple Yaler Web service written in C.

First, compile the example with

	make

or on Windows with

	Build

Now, create a free Yaler account at http://yaler.net/ to get a unique relay
domain for the Yaler instance hosted at try.yaler.net.

To start time_service on your computer, enter

	./time_service try.yaler.net <relay domain>

E.g., for the relay domain gsiot-ffmq-ttd5 type

	./time_service try.yaler.net gsiot-ffmq-ttd5

or on Windows

	time_service try.yaler.net gsiot-ffmq-ttd5

To access the service from everywhere, visit

	http://try.yaler.net/<relay domain>

In our example, time_service would be accessible at

	http://try.yaler.net/gsiot-ffmq-ttd5

If everything works fine, you should see a static Web page with the current time
on each reload, served right from your computer.
