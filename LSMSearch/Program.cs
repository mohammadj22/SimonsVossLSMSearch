using LSMSearch;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IFetchLsmDataService, FetchLsmDataService>();
builder.Services.AddSingleton<ISearchEngine, SearchEngine>();

const string myAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
	options.AddPolicy(name: myAllowSpecificOrigins,
		policy  =>
		{
			policy.WithOrigins("*").AllowAnyHeader();
		});
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapControllers();


app.UseHttpsRedirection();
app.UseCors(myAllowSpecificOrigins);

app.Run();