# Calamity Copy-Paste Bot
A Discord bot written in C# using Discord.NET to serve Calamain, froked from `tModLoader/tModLoader-Discord-Bot`. Uses .NET Core 2.0+

# Introductory
This bot is written in C# using .NET Core 2.0, with the Discord.NET library.
.NET Core runs natively on linux, this allows the bot to run as 24/7 service using a 1-XS x64 server.
The Discord.NET library makes it easy to develop bots using C#, and provides many features that enhance the bot itself. It also is built around asynchronous code design, which makes the bot itself _function better_ by design.

# Functions
This bot servers a few main purposes for our server.
Users can create tags. They tag be retrieved, edited and made global. Other users can also get global tags. Useful for storing information that is frequently given and otherwise needs to be typed. (Do note that this feature is only available to Cool Dudes, Devs, Trial & reg, Moderators, and Admins!)

Our bot also features the following:
1) A permission system (grant user/role based permission for commands or modules)
2) A configuration service on guild-to-guild basis that allows various configurations to be done by guild owners or assigned administrator
3) A sticky role feature that allows remembering of roles even if a user leaves the server and comes back later

# Exemplary
For sure this bot can be used as guidance on how to make a Discord bot, as well as how to utilize various Discord.NET features. However, keep in mind the license (both tModLoader-Discord-Bot and CCP use Apache 2.0). Feel free to learn from this bot, but please do not blatantly copy-paste this code.

# License
The default license is 'all-rights reserved'. This means, all rights to this work are reserved to its author(s). In this case, that's me, Jofairden. I made this bot, it is my code. The fact that it is open-source does not mean you can take my code and claim it as your own. Again, feel free to learn from it, but please be so polite to not steal it.

# No database
This bot does not use a database. Here is why. Using a database adds a certain complexity to your application you may or may not want to deal with. For us, we haven't seen many use-cases to be using a database. It sure is fast, and probably much faster than our IO-Read/Writes, however speed of these operations is of no importance to us for now. Not using a database allows for easy data handling as IO operations are innate to the programming language itself, and making data copies (including time-based back-ups) is very easy and scalable for files.

# Can I use this bot?
No. tModLoader-Discord-Bot was made specifically for tML and this fork was made specifically for Calamain.
