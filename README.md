# DeskCycle Speedometer

This project uses an Arduino to act as a speedometer for the DeskCycle, the same code can be used for anything that has a trigger when a wheel revolution completes.

Currently the Arduino monitors pin 7 and measures the time between revolutions to calculate RPM.  The RPM is reported to the serial port twice per second.
