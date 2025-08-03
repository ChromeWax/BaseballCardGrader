#include <Arduino.h>
#include <BLEDevice.h>
#include <BLEUtils.h>
#include <BLEServer.h>

// See the following for generating UUIDs:
// https://www.uuidgenerator.net/
#define DEVICE_NAME         "Baseball Card Grader Device"
#define SERVICE_UUID        "7123acc7-b24d-4eee-9c7f-ee6302637aef"
#define CHARACTERISTIC_UUID "8be0f272-b3be-4351-a3fc-d57341aa628e"

enum Command {
  UP,
  DOWN,
  LEFT,
  RIGHT,
  OFF
};

// define led according to pin diagram in article
const int upLedPin = D3;
const int downLedPin = D4;
const int leftLedPin = D5;
const int rightLedPin = D6;

const std::map<Command, int> commandToLedPin = {
  { UP, upLedPin },
  { DOWN, downLedPin },
  { LEFT, leftLedPin },
  { RIGHT, rightLedPin }
};

void setLedForCommand(Command command);

// callbacks for connecting and disconnecting BLE clients
class MyServerCallbacks: public BLEServerCallbacks {
    void onConnect(BLEServer* pServer) override {
    }
    void onDisconnect(BLEServer* pServer) override {
        // Restart advertising so clients can reconnect
        BLEAdvertising *pAdvertising = pServer->getAdvertising();
        pAdvertising->start();
    }
};

// callbacks for handling commands
class MyCharacteristicCallbacks: public BLECharacteristicCallbacks {
    void onWrite(BLECharacteristic *pCharacteristic) {
      std::string value = pCharacteristic->getValue();

      if (value.length() > 0) {
        Command command;
        if (value == "up") command = UP;
        else if (value == "down") command = DOWN;
        else if (value == "left") command = LEFT;
        else if (value == "right") command = RIGHT;
        else if (value == "off") command = OFF;
        else return;

        setLedForCommand(command);
      }
    }
};

void setLedForCommand(Command command) {
  // Set all LEDs LOW
  digitalWrite(upLedPin, LOW);
  digitalWrite(downLedPin, LOW);
  digitalWrite(leftLedPin, LOW);
  digitalWrite(rightLedPin, LOW);

  // Set the selected LED HIGH if valid
  auto entry = commandToLedPin.find(command);
  if (entry != commandToLedPin.end()) {
    digitalWrite(entry->second, HIGH);
  }
}

void setup() {
  Serial.begin(115200);

  // initialize digital pin led as an output
  pinMode(upLedPin, OUTPUT);
  pinMode(downLedPin, OUTPUT);
  pinMode(leftLedPin, OUTPUT);
  pinMode(rightLedPin, OUTPUT);

  // turn off all LEDs initially
  setLedForCommand(Command::OFF);

  BLEDevice::init(DEVICE_NAME);
  BLEServer *pServer = BLEDevice::createServer();
  pServer->setCallbacks(new MyServerCallbacks());

  BLEService *pService = pServer->createService(SERVICE_UUID);
  BLECharacteristic *pCharacteristic = pService->createCharacteristic(
                                         CHARACTERISTIC_UUID,
                                         BLECharacteristic::PROPERTY_READ |
                                         BLECharacteristic::PROPERTY_WRITE
                                       );
  pCharacteristic->setCallbacks(new MyCharacteristicCallbacks());
  pService->start();

  BLEAdvertising *pAdvertising = pServer->getAdvertising();
  pAdvertising->start();
}

void loop() {
  delay(2000);
}