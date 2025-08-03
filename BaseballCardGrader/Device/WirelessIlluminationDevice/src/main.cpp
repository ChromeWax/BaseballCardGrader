#include <Arduino.h>

// define led according to pin diagram in article
const int upLed = D3;
const int downLed = D4;
const int leftLed = D5;
const int rightLed = D6;

void setup() {
  // initialize digital pin led as an output
  pinMode(upLed, OUTPUT);
  pinMode(downLed, OUTPUT);
  pinMode(leftLed, OUTPUT);
  pinMode(rightLed, OUTPUT);
}

void loop() {
  digitalWrite(upLed, HIGH);   // turn the LED on 
  digitalWrite(downLed, LOW);   // turn the LED on 
  digitalWrite(leftLed, LOW);   // turn the LED on 
  digitalWrite(rightLed, LOW);   // turn the LED on 
  delay(100);               // wait for a second
  digitalWrite(upLed, LOW);   // turn the LED on 
  digitalWrite(downLed, HIGH);   // turn the LED on 
  digitalWrite(leftLed, LOW);   // turn the LED on 
  digitalWrite(rightLed, LOW);   // turn the LED on 
  delay(100);               // wait for a second
  digitalWrite(upLed, LOW);   // turn the LED on 
  digitalWrite(downLed, LOW);   // turn the LED on 
  digitalWrite(leftLed, HIGH);   // turn the LED on 
  digitalWrite(rightLed, LOW);   // turn the LED on 
  delay(100);               // wait for a second
  digitalWrite(upLed, LOW);   // turn the LED on 
  digitalWrite(downLed, LOW);   // turn the LED on 
  digitalWrite(leftLed, LOW);   // turn the LED on 
  digitalWrite(rightLed, HIGH);   // turn the LED on 
  delay(100);               // wait for a second
}