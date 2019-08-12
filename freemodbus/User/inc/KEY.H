#ifndef _KEY_H_
#define _KEY_H_
#include "stm32f10x.h"

#define S1 GPIO_ReadInputDataBit(GPIOC,GPIO_Pin_2)
#define S2 GPIO_ReadInputDataBit(GPIOC,GPIO_Pin_3)
#define S3 GPIO_ReadInputDataBit(GPIOC,GPIO_Pin_5)


void KeyInit(void);//按键相关IO初始化



#endif
