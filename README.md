# DeskCycle Speedometer

This project uses an Arduino to act as a speedometer for the DeskCycle (www.deskcycle.com), the same code can be used for anything that has a trigger when a wheel revolution completes.

Currently the Arduino monitors pin 7 and measures the time between revolutions to calculate RPM and Speed.  The client application requests data by writing the following chars to the serial port;
h	handshake - Arduino returns "DeskCycle Speedo", enables the client to search COM ports for the speedo
s	speed - returns the speed in MPH
c	cadence - returns the RPM of the pedals
b	both - returns "speed,cadence"