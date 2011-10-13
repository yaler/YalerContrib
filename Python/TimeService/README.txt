TimeService is an example of a simple Yaler Web service written in Python.

First, create a free Yaler account at http://yaler.net/ to get a unique relay
domain for the Yaler instance hosted at try.yaler.net.

Then start TimeService on your computer with

	TimeService.py try.yaler.net <relay domain>

E.g., for the relay domain gsiot-ffmq-ttd5 enter

	TimeService.py try.yaler.net gsiot-ffmq-ttd5

To access the service from everywhere, visit

	http://try.yaler.net/<relay domain>

In our example, TimeService would be accessible at

	http://try.yaler.net/gsiot-ffmq-ttd5

If everything works fine, you should see a static Web page with the actual time
on each reload, served right from your computer.
