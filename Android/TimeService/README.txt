TimeService is an example of a simple Yaler Web service for Android.

First, create a Yaler account at https://yaler.net/ to get a unique relay
domain for the Yaler instance hosted at try.yaler.io.

Then open the example Eclipse project and change the constant RELAY_DOMAIN in
the file TimeService.java to your relay domain.

Connect the Android device to your computer and press Run in Eclipse.

To access the service from everywhere, visit

	http://RELAY_DOMAIN.try.yaler.io/

For example, given the relay domain gsiot-ffmq-ttd5, TimeService would be
accessible at

	http://gsiot-ffmq-ttd5.try.yaler.io/

If everything works fine, you should see a static Web page with the actual time
on each reload, served right from your Android device.
