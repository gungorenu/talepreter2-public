# Welcome

this is a custom hobby project for myself, nothing special

it is called **Talepreter**, an interpreter for tales which could be used for other purposes, such as movie making or other scenario required objects (books and novels mainly).

it is not entire code, and it is expected to fail to build, *on purpose*, should require some secret assemblies from private nuget repo in github. but rest is as is.

it also has a WPF based GUI but not here due to some reasons. maybe it will also be added later

## V2

this is new version of former project (https://github.com/gungorenu/talepreter-public). it is redesigned on some parts as the techstack would fit better. the notion of learning the tech is fulfilled so I can use the best decisions now. many parts are different compared to former version, some shortcuts removed, some design problems solved. the main difference is focusing more on Mongodb and less EntityFramework, entities are strongly types as plugin system is removed. another difference is preparing for a web GUI. right now it is available with old WPF and new Web client (it reads data from Mongodb directly so it works almost with %98 same code). Performance looks better although still not within my expectations. it is still an ongoing project anyway. 

## What does it do?

Talepreter is a helper app to summarize what has happened in the tale, especially useful after maybe hundreds of pages. it stores information about actors, anecdotes (which could be anything), NPCs (non-actors), settlements and some world information. when I am writing *next* page, I need to know what has happened before, and if tale is very long then sometimes important information might be skipped. it would create inconsistencies if writer does not follow what has been mentioned before in former pages. 

Ex: if actor X does not smoke (maybe never done in his life), in future pages tale should not mention that "X smokes a cigarette". or similarly if actor Y does not have a driving license, in future pages tale should not mention him driving a car (for sure there might be exceptions like he is driving without a license, escaping from bad guys etc.); consistency would be broken for such cases. maybe a settlement had a landslide some time ago and many things changed there, so in new pages the former information must be considered. if tale is long (like telling a tale of hundred years) then some people shall pass away eventually. it would be weird to mention actor X never ages and his mother is still living beyond limits of human life (unless the world has different rules). 

this app will **help** (not fix things automatically) writer on such topics given above. writer can check for many details before writing the next page for the tale. at least it can look at Talepreter views to think and focus on real content instead of digging continuously to check if something is broken. for my own hobby, app does many other things too (many calculations) which I do not want to bother with. this does not mean it is limited to such features only but a starter for now.

# Tech Stack

pure new stuff if possible, also some of them are new for me. I am a developer, not devops guy, so as long as stuff works, that is good enough.

* uses netcore 8 mainly
* uses Orleans, RabbitMQ, EntityFramework, MongoDB, Rest API
* orchestration shall be basic docker-compose but maybe kubernetes yml will be added later. everything is in container, including web interface (except WPF GUI)
* DB is SqlServer (MSSQL) and MongoDB, uses both in docker

GUI is both an Angular (MEAN stack) web and WPF targeting windows. Talepreter runs in docker linux containers (5+2 service plus infrastructure for now) and GUI will be at windows due to some hard requirements related to filesystem. 

# Weird Stuff

this is a hobby project, most of the things are done on purpose to be faster developed. if it is working good *enough*, then I am happy *enough*. if it works on my machine that means it is done.

some parts of code may not be visible. this is on purpose.

my content (content of a tale, scenario for example) is always in a file system supported by a source control, so losing entire data in Talepreter is very normal (part of an action I do perhaps daily, not about development). the entire data will be read from file system and regenerated again (and again and again), which might be a little weird compared to other apps in other businesses.

> when I write a tale, I continuously go back and add stuff to former pages. from an author perspective, it is very weird. a completed page must not be touched, but I do because I am not a good writer. this makes me forced to build views again and again, sometimes disconnect parts of the tale to rewrite some time later. it means a (even first) page is never complete, even after I am writing thousandth page. next day I might go back to a former page, change it, and have to see new results. this means entire view data must be regenerated again. that is the main use case for the app.

some data modeling is weird but it is done on purpose. the content of a tale is in text and the notes coming from a content must be humanly readable/writable (even json is very unreadable for that), so there are many generic rules to support all kinds of notes (I call them page command). it is not a good idea for a system like that perhaps but again the main purpose is my own usage and I write tales in markdown, so design of app is very affected by that too, simple text processing mainly.

## Simplification

there are some places where the application does weird things but also I could not find a better way. the source of commands in a tale is single and same for every service. so same command will go to four services to be processed. problem starts there, not every service is interested in those commands, not all. as a designer of each service I know which commands will be executed where but still response system has to be fast and not blocking controller grains. 

## Performance Concerns

there are some parts which I have concerns about, performance of grains and being blocked by common DB. in my previous application everything was in memory but also process was done in single thread. since it was single application, I did not need to have a concept of communication. now with multiple services, there is a concept and furthermore these worker entities are orleans actors, which means they will be blocked down to single thread sometimes. I have taken some shortcuts about validation to make things faster already. full publish operation in old application takes up to 3 seconds in my sample tale of 30 chapters/660 pages/7k page commands. that means 7k x2 message will be processed during a single publish operation. same grains will be called many times over things to update (about progress) and response handling. due to this x3 message handling I focused on progress response handling, like how much is done within a second, maybe how long a full publish takes. 

there could be other ways to handle this too, but I chose that way to see if it is that bad or negligible. one of the open issue is writing (upload operation) page commands, they go directly into services due to single tale is affected by this, and chapter/page grains are only used to process/execute responses, not write. the very obvious solution would be not to upload everything again but the problem comes from changes I do naturally. I change past of the pages so most operations (process/execute) has to be done again. that direction (changing what is needed only) will be next version I will implement, and maybe go with full actor model depending on performance. 
