#ifndef _LED_H_
#define _LED_H_
#include "stm32f10x.h"

enum{YES,NO};
#define IsLed1On(x) (x)?GPIO_SetBits(GPIOD, GPIO_Pin_3):GPIO_ResetBits(GPIOD, GPIO_Pin_3);
#define IsLed2On(x) (x)?GPIO_SetBits(GPIOD, GPIO_Pin_6):GPIO_ResetBits(GPIOD, GPIO_Pin_6);	
#define IsLed3On(x) (x)?GPIO_SetBits(GPIOB, GPIO_Pin_5):GPIO_ResetBits(GPIOB, GPIO_Pin_5);
//#define IsLed4On(x) (x)?GPIO_SetBits(GPIOC, GPIO_Pin_7):GPIO_ResetBits(GPIOC, GPIO_Pin_7);

void LedInit(void);//LED相关IO初始化
void LedTest(void);
#endif
