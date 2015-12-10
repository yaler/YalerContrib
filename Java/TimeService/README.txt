TimeService is an example of a simple Yaler Web service written in Java.

First, compile the example source with

	javac TimeService.java

Now, create a Yaler account at https://yaler.net/ to get a unique relay
domain for the Yaler instance hosted at try.yaler.io.

Then start TimeService on your computer with

	java TimeService try.yaler.io RELAY_DOMAIN

E.g., for the relay domain gsiot-ffmq-ttd5 enter

	java TimeService try.yaler.io gsiot-ffmq-ttd5

To access the service from everywhere, visit

	http://RELAY_DOMAIN.try.yaler.io/

In our example, TimeService would be accessible at

	http://gsiot-ffmq-ttd5.try.yaler.io/

If everything works fine, you should see a static Web page with the actual time
on each reload, served right from your computer.
