#include "KEY.H"

void KeyInit(void)//按键相关IO初始化
{
	GPIO_InitTypeDef GPIO_InitStructure;

	RCC_APB2PeriphClockCmd( RCC_APB2Periph_GPIOC, ENABLE);//使能GPIOA,GPIOD,GPIOE时钟

	GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IPU;//上拉输入
	GPIO_InitStructure.GPIO_Pin = GPIO_Pin_2|GPIO_Pin_3|GPIO_Pin_5;		
	GPIO_Init(GPIOC, &GPIO_InitStructure);//初始化GPIOE0


}


