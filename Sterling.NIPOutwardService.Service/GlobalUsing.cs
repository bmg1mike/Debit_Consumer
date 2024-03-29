global using AutoMapper;
global using Sterling.NIPOutwardService.Data.Repositories.Interfaces;
global using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos;
global using Sterling.NIPOutwardService.Domain.Entities;
global using Microsoft.Extensions.DependencyInjection;
global using Sterling.NIPOutwardService.Service.Services.Implementations;
global using Sterling.NIPOutwardService.Service.Services.Interfaces;
global using Microsoft.Extensions.Configuration;
global using Sterling.NIPOutwardService.Domain.Common;
global using Sterling.NIPOutwardService.Domain.DataTransferObjects.DtoValidators;
global using FluentValidation.Results;
global using Serilog;
global using Sterling.NIPOutwardService.Domain;
global using Microsoft.AspNetCore.Authentication.JwtBearer;
global using System.Text;
global using Microsoft.IdentityModel.Tokens;
global using Microsoft.Extensions.Options;
global using Sterling.NIPOutwardService.Domain.Config.Implementations;
global using MongoDB.Bson;
global using Newtonsoft.Json;
global using Sterling.NIPOutwardService.Data.Repositories.Interfaces.NIPOutwardLookup;
global using Sterling.NIPOutwardService.Domain.Entities.NIPOutwardLookup;
global using Sterling.NIPOutwardService.Service.Services.Interfaces.NIPOutwardLookup;
global using Polly;
global using Polly.Retry;
global using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.FraudAnalytics;
global using Sterling.NIPOutwardService.Data.Repositories.Interfaces.TransactionAmountLimits;
global using Sterling.NIPOutwardService.Domain.Entities.TransactionAmountLimits;
global using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.VTeller;
global using Confluent.Kafka;
global using Sterling.NIPOutwardService.Service.Services.Interfaces.Kafka;
global using Polly.Extensions.Http;
global using Sterling.NIPOutwardService.Service.Services.Implementations.Kafka;
global using Sterling.NIPOutwardService.Service.Services.Implementations.NIPOutwardLookup;
global using Sterling.NIPOutwardService.Data.Helpers.Interfaces;
global using System.ServiceModel;
global using System.Xml;
global using NIBBSNIPService;
global using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.NameEnquiry;
global using Sterling.NIPOutwardService.Service.Helpers.Interfaces;
global using Sterling.NIPOutwardService.Data.Repositories.Implementations;
global using Sterling.NIPOutwardService.Service.Helpers.Implementations;
global using Sterling.NIPOutwardService.Service.Services.Interfaces.ExternalServices;
global using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.WalletFraudAnalytics;
global using static System.Net.Mime.MediaTypeNames;
global using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.WalletToWallet;
global using Microsoft.AspNetCore.Http;
global using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.TransactionValidation;
global using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.ImalTransaction;


 



