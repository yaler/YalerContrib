TimeService is an example of a simple Yaler Web service for Android 2.2.

Download the Yaler source from http://yaler.org/ and build it as described in
README.txt. Then start the Yaler relay server with 

        java -ea -cp yaler.jar org.yaler.Yaler <yaler_host>:8081 

Note that Yaler has to be hosted on port 8081 on a computer with a public IP to be reachable from the Android device.

Open the example Eclipse project and change YALER_HOST in the file TimeService.java to be your actual host.

Connect the Android device to your computer and press Run in Eclipse.

To access the service on your computer, visit

	http://<yaler_host>:8081/my-android/

with your browser. If it works, you should see a static Web page with the actual
time on each reload.