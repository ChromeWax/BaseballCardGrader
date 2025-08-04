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
  NONE,
  UP,
  DOWN,
  LEFT,
  RIGHT
};

// define led according to pin diagram in article
const int wakePin = D0;
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

const int oneSecond = 1000;
const int oneMinute = oneSecond * 60;
const int sleepTime = oneMinute * 3;

// function declarations
Command parseCommand(const std::string& value);
void setAllLedsOff();
void enableLedByCommandForOneSecond(Command command);
void goToSleep();

// global variables
BLEAdvertising *pAdvertising = nullptr;
unsigned long lastActivityTime = 0;

// callbacks for connecting and disconnecting BLE clients
class MyServerCallbacks: public BLEServerCallbacks {
    void onConnect(BLEServer* pServer) override {
        lastActivityTime = millis();
    }
    void onDisconnect(BLEServer* pServer) override {
        // Restart advertising so clients can reconnect
        if (pAdvertising) {
            pAdvertising->start();
        }
        lastActivityTime = millis();
    }
};

// callbacks for handling commands
class MyCharacteristicCallbacks: public BLECharacteristicCallbacks {
    void onWrite(BLECharacteristic *pCharacteristic) {
      std::string value = pCharacteristic->getValue();

      if (value.length() > 0) {
        Command command = parseCommand(value);
        if (command == Command::NONE) return;

        enableLedByCommandForOneSecond(command);
        lastActivityTime = millis();
      }
    }
};

Command parseCommand(const std::string& value) {
    if (value == "up") return Command::UP;
    if (value == "down") return Command::DOWN;
    if (value == "left") return Command::LEFT;
    if (value == "right") return Command::RIGHT;
    return Command::NONE;
}

void setAllLedsOff() {
  digitalWrite(upLedPin, LOW);
  digitalWrite(downLedPin, LOW);
  digitalWrite(leftLedPin, LOW);
  digitalWrite(rightLedPin, LOW);
}

void enableLedByCommandForOneSecond(Command command) {
  setAllLedsOff();

  // Set the selected LED HIGH if valid
  auto entry = commandToLedPin.find(command);
  if (entry != commandToLedPin.end()) {
    digitalWrite(entry->second, HIGH);
    delay(oneSecond); 
    digitalWrite(entry->second, LOW);
  }
}

void goToSleep() {
  setAllLedsOff();
  if (pAdvertising) pAdvertising->stop();
  esp_deep_sleep_enable_gpio_wakeup(BIT(wakePin), ESP_GPIO_WAKEUP_GPIO_LOW);
  esp_deep_sleep_start();
}

void setup() {
  Serial.begin(115200);

  // initialize digital pin led as an output
  pinMode(upLedPin, OUTPUT);
  pinMode(downLedPin, OUTPUT);
  pinMode(leftLedPin, OUTPUT);
  pinMode(rightLedPin, OUTPUT);

  // turn off all LEDs initially
  setAllLedsOff();

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

  pAdvertising = pServer->getAdvertising();
  pAdvertising->addServiceUUID(SERVICE_UUID);
  pAdvertising->start();

  lastActivityTime = millis();
}

void loop() {
  auto elapsedTime = millis() - lastActivityTime;
  if (elapsedTime > sleepTime) {
    goToSleep();
  }
  delay(100);
}
