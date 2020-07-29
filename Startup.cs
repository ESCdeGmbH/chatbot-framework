using Framework.Controller;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Framework
{
    /// <summary>
    /// This class is the entry point of the app.
    /// </summary>
    public abstract class Startup
    {
        /// <summary>
        /// The global logger factory.
        /// </summary>
        protected ILoggerFactory _loggerFactory;
        /// <summary>
        /// Indicates wheter the program runs in productive mode or not.
        /// </summary>
        protected bool _isProduction = false;

        /// <summary>
        /// Indicator to use SignalR. Default endpoint is "/receiverhub".
        /// </summary>
        protected bool _useSignalR;

        /// <summary>
        /// Starts the bot in the environment.
        /// </summary>
        /// <param name="env">The hosting environment.</param>
        /// <param name="useSignalR">Indicator to use SignalR. Default endpoint is "/receiverhub".</param>
        public Startup(IWebHostEnvironment env, bool useSignalR = false)
        {
            _useSignalR = useSignalR;
            _isProduction = env.IsProduction();
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        /// <summary>
        /// Gets the configuration that represents a set of key/value application configuration properties.
        /// </summary>
        /// <value>
        /// The <see cref="IConfiguration"/> that represents a set of key/value application configuration properties.
        /// </value>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container. By default this will invoke <see cref="ConfigureDefaultServices(IServiceCollection)"/>
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> specifies the contract for a collection of service descriptors.</param>
        /// <seealso cref="IStatePropertyAccessor{T}"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/dependency-injection"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-channels?view=azure-bot-service-4.0"/>
        public virtual void ConfigureServices(IServiceCollection services) => ConfigureDefaultServices(services);

        /// <summary>
        /// Configures the services with default parameters.
        /// </summary>
        /// <param name="services">the services of this bot</param>
        protected void ConfigureDefaultServices(IServiceCollection services) => ConfigureDefaultServicesByFrameworkAdapter<AdapterWithErrorHandler>(services);


        /// <summary>
        /// Same as <see cref="ConfigureDefaultServices(IServiceCollection)"/> but use a specific bot framework adapter instance
        /// </summary>
        /// <param name="services">the services of this bot</param>
        /// <param name="specificAdapterInstance">the specific adapter instance</param>
        /// <typeparam name="A">the type of the <see cref="IBotFrameworkHttpAdapter"/></typeparam>
        protected void ConfigureDefaultServicesByFrameworkAdapter<A>(IServiceCollection services) where A : class, IBotFrameworkHttpAdapter
        {
            services.AddControllers().AddNewtonsoftJson();
            services.AddSingleton<IBotFrameworkHttpAdapter, A>();

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();
            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();
            if (_useSignalR) services.AddSignalR();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The hosting environment.</param>
        /// <param name="loggerFactory">the global logger factory.</param>
        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseWebSockets()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    if (_useSignalR) endpoints.MapHub<OfflineHub>("/receiverhub");
                });

        }
    }
}
