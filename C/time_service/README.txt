time_service is an example of a simple Yaler Web service written in C.

First, compile the example with

	make

or on Windows with

	Build

Now, create a Yaler account at https://yaler.net/ to get a unique relay
domain for the Yaler instance hosted at try.yaler.io.

To start time_service on your computer, enter

	./time_service try.yaler.io RELAY_DOMAIN

E.g., for the relay domain gsiot-ffmq-ttd5 type

	./time_service try.yaler.io gsiot-ffmq-ttd5

or on Windows

	time_service try.yaler.io gsiot-ffmq-ttd5

To access the service from everywhere, visit

	http://RELAY_DOMAIN.try.yaler.io/

In our example, time_service would be accessible at

	http://gsiot-ffmq-ttd5.try.yaler.io/

If everything works fine, you should see a static Web page with the current time
on each reload, served right from your computer.
