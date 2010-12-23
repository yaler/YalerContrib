TimeService is an example of a simple Yaler Web service written in Python 2.7.

Download the Yaler source from http://yaler.org/ and build it as described in
README.txt. Then start the Yaler relay server with

	java -ea -cp yaler.jar org.yaler.Yaler 127.0.0.1:80

Now, start the service on your computer with

	TimeService.py 127.0.0.1 my-computer

To access the service on your computer, visit

	http://127.0.0.1:80/my-computer/

with your browser. If it works, you should see a static Web page with the actual
time on each reload. To get the full Yaler experience (access from any browser),
you'll have to host the relay server on a separate computer with a public IP
address and adapt all occurrences of 127.0.0.1 above and in the example source.
