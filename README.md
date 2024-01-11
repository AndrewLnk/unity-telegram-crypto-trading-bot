# Unity Telegram Crypto Trading Bot for Kucoin
* This is a Telegram bot for automated cryptocurrency trading on 1 pair.
* You can run the bot on a remote machine and use it all the time.
* APIs from KuCoin, a Telegram bot created via BotFather, Firebase for storing state will be useful for launching.
* The bot can be run on any platform.
* The engine used is Unity.
* Used additional packages: <i>cryptoexchange.net, kucoin.net, telegram.bot</i>.

____


## Trading mechanism
This bot analyzes incoming ticks every 10 seconds and when the value of the secondary currency falls, it expects the chart to break with a certain accuracy. When the chart breaks to the upward trend, a purchase for a certain amount is made. The purchased secondary coins will be automatically sold when the value rises by a certain percentage of the purchase. Selling is also done through analyzing the growth chart and waiting for the maximum price.

At a low price secondary coins will be bought and sold at some percentage higher (buying package). Each package will be sold separately at high price.
All bot settings and buying packages are stored in Firebase Storage.
Buying can be done manually and selling packages as well.

____

## Bot trading settings
`Analyze Min Buy` - Analyze the chart over a period of time for a buy signal.    
`Analyze Min Sell` - Analyze the chart over a period of time for a sell signal.    
`Analyze Max` - Maximum Period Analysis.    

`Anchor` - The percentage of the secondary coin drop below which a purchase occurs.    
`Anchor Duration` - The period after which the price will be re-analyzed for purchase.    
`Absolute Buy` - Percentage of chart decline when a purchase is sure to be made without waiting for a trend change.    
`Lock When Bought` - No new purchase will be made during this time after a previous purchase.    

`Sell` - An increase in the percentage of the purchase price when a sale is made.    
`Absolute Sell` - A price above this proent will sell a bundle of purchased coins.    

`Stop Price` - If the price is lower than the specified price, no purchases will be made.    
`Limit Price` - If the price is higher than the specified price, no purchases will be made.    

`Exchange Transfer` - How many coins of the primary will be spent to buy the secondary coin.    
`Reserve` - How many primary coins to keep anyway.    

![Alt-text](https://i.ibb.co/r2F18hp/4.png "Bot Settings")

____

## Step-by-step setup guide
1. Install any recent version of Unity and open this project in it. The project is built under version 2021.3.11f1.
2. Customize user data in the <i>Assets\Scripts\UserData.cs</i> script.
   
   -  Data for interacting with the Kucoin platform. It is necessary to create an API and add it to the script:
         
      :white_check_mark: `API Key`    
      :white_check_mark: `API Secret`    
      :white_check_mark: `API Password`

   - Choose a pair to trade on Spot. Usually a stable coin is chosen and a secondary coin is chosen to buy/sell.
             
      :dollar: `DefaultCoin`    
      :money_with_wings: `SecondaryCoin`

   - In Telegram Bot <i>https://t.me/BotFather</i> create your own bot in which to interact with the process. After creation, you will need to add the API Token to the script.
              
      :speech_balloon: `TelegramToken`    

    - Come up with a password to log into the bot.
          
      :closed_lock_with_key: `TelegramToken`

    - After that, in Google Firebase, create a new project with the Storage module. In the script add the project address, which can be found in the Storage module.
          
      :floppy_disk: `FirebaseStorageUrl`

![Alt-text](https://i.ibb.co/cxkWXsj/5.png)

3. After configuring, run the project either in the Unity editor or the finished build on a remote machine. As a result, the bot in Telegram will work.

____

## Usage
The bot is intuitive. The trading process is carried out when the bot is started.

____

## Contacts
Email: andrew.olenk@gmail.com
Support: THW4xRFC7N76Hofopt9TZhynFV76PnEarY (USDT TRC20)
