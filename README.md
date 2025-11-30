## KeyCloak .NET Microservice project with Aspire as infra. orchestractor

Below you can find the step-by-step tutorial on how the infrastructure of this project was developed.
The goal of this project is to create a "Stack Overflow" project using <strong>Keycloak</strong> as Auth provider, <strong>RabbitMQ</strong> as event bus (communication protocol between microservices), PostgreSQl as our DB, MongoDB for our search service using **TypeSense** and **NextJS** as our frontend. <br/><br/>
*This project has only one realm - "overflow" realm.*

Technologies: <br/>
<img src="https://upload.wikimedia.org/wikipedia/commons/thumb/b/b4/Logo_of_Keycloak.svg/2560px-Logo_of_Keycloak.svg.png" alt="keycloak" width="auto" height="50px" style="object-fit: cover;"/>
<img src="https://images.g2crowd.com/uploads/product/image/82441b528e62ce65d54920edbc10c00e/typesense.png" alt="typesense" width="auto" height="50px" style="object-fit: cover;"/>
<img src="https://upload.wikimedia.org/wikipedia/commons/thumb/7/71/RabbitMQ_logo.svg/2560px-RabbitMQ_logo.svg.png" alt="rabbitmq" width="auto" height="50px" style="object-fit: cover;"/>
<img src="https://images.icon-icons.com/2415/PNG/512/postgresql_plain_wordmark_logo_icon_146390.png" alt="postgresql" width="auto" height="50px" style="object-fit: cover;"/>
<img src="https://upload.wikimedia.org/wikipedia/commons/thumb/9/93/MongoDB_Logo.svg/2560px-MongoDB_Logo.svg.png" alt="mongodb" width="auto" height="50px" style="object-fit: cover;"/>
<img src="https://upload.wikimedia.org/wikipedia/commons/thumb/7/7d/Microsoft_.NET_logo.svg/2048px-Microsoft_.NET_logo.svg.png" alt=".net" width="auto" height="50px" style="object-fit: cover;"/>
<img src="https://www.svgrepo.com/show/354113/nextjs-icon.svg" alt="nextjs" width="auto" height="50px" style="object-fit: cover;"/>

# Prerequisite

Before you start developing this project, you must have installed:
1. Docker Desktop
2. .NET 9 SDK


# Project setup

Open you terminal and check out if you have .NET 9 installed. <br/>
If you don't, download and install it from this link https://dotnet.microsoft.com/en-us/download/dotnet/9.0  <br/>
*This is necessary for running this project which is coded on top of .NET 9.*

After installation, run this command to download and install Aspire templates <br/>
``` dotnet new install Aspire.ProjectTemplates ```

Create a directory where this project will be located and using you IDE or CLI, create new <strong>Blank Aspire</strong> project. <br/>
Use Aspire 9.xx. versions for this project as we are using .NET 9. 

Last thing, install the necessary NuGet package for setting up and running Keycloak. NuGet package<strong>Aspire.Hosting.Keycloak</strong>.
<br/>
That is it, our project is now setup for development.


