﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;
using MTNetCoreData.Entities;

namespace MT_NetCore_Common.Utilities
{
    /// <summary>
    /// The class which performs all tasks related to sharding
    /// </summary>
    public class Sharding
    {
        #region Private declarations
        //private static IUtilities _utilities;
        //private static ICatalogRepository _catalogRepository;
        //private static ITenantRepository _tenantRepository;
        #endregion

        #region Public Properties

        public ShardMapManager ShardMapManager { get; }

        public static ListShardMap<int> ShardMap { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Sharding" /> class.
        /// <para>Bootstrap Elastic Scale by creating a new shard map manager and a shard map on the shard map manager database if necessary.</para>
        /// </summary>
        /// <param name="catalogDatabase">The catalog database.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="catalogRepository">The catalog repository.</param>
        /// <param name="tenantRepository">The tenant repository.</param>
        /// <param name="utilities">The utilities class.</param>
        /// <exception cref="System.ApplicationException">Error in sharding initialisation.</exception>
        public Sharding(string catalogDatabase, string connectionString, ICatalogRepository catalogRepository, ITenantRepository tenantRepository, IUtilities utilities)
        {
            try
            {
                //_catalogRepository = catalogRepository;
                //_tenantRepository = tenantRepository;
                //_utilities = utilities;

                // Deploy shard map manager
                // if shard map manager exists, refresh content, else create it, then add content
                ShardMapManager smm;
                ShardMapManager =
                    !ShardMapManagerFactory.TryGetSqlShardMapManager(connectionString,
                        ShardMapManagerLoadPolicy.Lazy, out smm)
                        ? ShardMapManagerFactory.CreateSqlShardMapManager(connectionString)
                        : smm;

                // check if shard map exists and if not, create it 
                ListShardMap<int> sm;
                ShardMap = !ShardMapManager.TryGetListShardMap(catalogDatabase, out sm)
                    ? ShardMapManager.CreateListShardMap<int>(catalogDatabase)
                    : sm;
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.Message, "Error in sharding initialisation.");
            }

        }

        #endregion

        #region Public methods

        /// <summary>
        /// Verify if shard exists for the tenant. If not then create new shard
        /// </summary>
        /// <param name="tenantName">Name of the tenant.</param>
        /// <param name="tenantServer">The tenant server.</param>
        /// <param name="databaseServerPort">The database server port.</param>
        /// <param name="servicePlan">The service plan.</param>
        /// <returns></returns>
        public static Shard CreateNewShard(string tenantName, string tenantServer, int databaseServerPort, string servicePlan)
        {
            Shard shard;
            try
            {
                ShardLocation shardLocation = new ShardLocation(tenantServer, tenantName, SqlProtocol.Tcp, databaseServerPort);
                if (!ShardMap.TryGetShard(shardLocation, out shard))
                {
                    //create shard if it does not exist
                    shard = ShardMap.CreateShard(shardLocation);
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.Message, "Error in registering new shard.");
                shard = null;
            }
            return shard;
        }

        /// <summary>
        /// Registers the new shard and add tenant details to Tenants table in catalog
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="databaseServerPort">The database server port.</param>
        /// <param name="servicePlan">The service plan.</param>
        /// <param name="shard">The shard which needs to be registered.</param>
        /// <returns></returns>
        public static async Task<bool> RegisterNewShard(int tenantId, string servicePlan, Shard shard)
        {
            try
            {
                // Register the mapping of the tenant to the shard in the shard map.
                // After this step, DDR on the shard map can be used
                PointMapping<int> mapping;
                if (!ShardMap.TryGetMappingForKey(tenantId, out mapping))
                {
                    var pointMapping = ShardMap.CreatePointMapping(tenantId, shard);

                    //convert from int to byte[] as tenantId has been set as byte[] in Tenants entity
                    //var key = _utilities.ConvertIntKeyToBytesArray(pointMapping.Value);

                    //get tenant's venue name
                    //var venueDetails = await _tenantRepository.GetVenueDetails(tenantId);

                    //add tenant to Tenants table
                    var tenant = new Tenant
                    {
                        ServicePlan = servicePlan,
                        TenantId = key,
                        TenantName = venueDetails.VenueName
                    };

                    _catalogRepository.Add(tenant);
                }
                return true;
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.Message, "Error in registering new shard.");
                return false;
            }
        }

        #endregion

    }
}
