angular.module('virtoCommerce.searchAPIModule')
.factory('virtoCommerce.searchAPIModule.searchAPIResources', ['$resource', function ($resource) {
    return $resource('api/searchApi/:id', null, {
        getStatistics: { url: 'api/searchApi/statistics/:catalogId' },
        buildIndex: { method: 'PUT' }
    });
}]);
