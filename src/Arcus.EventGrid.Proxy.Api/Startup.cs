﻿using Arcus.EventGrid.Proxy.Api.Extensions;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using Arcus.EventGrid.Proxy.Api.Validation;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arcus.EventGrid.Proxy.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .AddJsonOptions(jsonOptions =>
                {
                    jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    jsonOptions.JsonSerializerOptions.IgnoreNullValues = true;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.UseOpenApiSpecifications();
            services.AddSingleton(BuildEventGridPublisher);

            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            var validationOutcomes = RuntimeValidator.Run(Configuration);

            if (validationOutcomes.Any(validationOutcome => validationOutcome.Successful == false))
            {
                throw new Exception("Unable to start up due to invalid configuration");
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseOpenApiDocsWithExplorer();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }

        private IEventGridPublisher BuildEventGridPublisher(IServiceProvider serviceProvider)
        {
            var rawTopicEndpoint = Configuration[EnvironmentVariables.Runtime.EventGrid.TopicEndpoint];
            var authenticationKey = Configuration[EnvironmentVariables.Runtime.EventGrid.AuthKey];
            var topicUri = new Uri(rawTopicEndpoint);
            return EventGridPublisherBuilder.ForTopic(topicUri)
                .UsingAuthenticationKey(authenticationKey)
                .Build();
        }
    }
}