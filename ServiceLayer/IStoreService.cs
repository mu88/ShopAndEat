﻿using System.Collections.Generic;
using DTO.Store;

namespace ServiceLayer
{
    public interface IStoreService
    {
        IEnumerable<ExistingStoreDto> GetAllStores();
    }
}