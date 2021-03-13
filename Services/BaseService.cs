using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace tModloaderDiscordBot.Services
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public abstract class BaseService : IBotService
	{
		protected readonly LoggingService _loggingService;

		protected BaseService(IServiceProvider services) =>
			_loggingService = services.GetRequiredService<LoggingService>();
	}
}